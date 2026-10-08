using Monitel.DataContext.Tools.AllowedTree;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions;
using Monitel.Mal.Snapshot;
using Monitel.PlatformInfrastructure.Logger;
using Monitel.Protocol.Common;
using Monitel.Serialization.CIMXML;
using Monitel.Serialization.CIMXML.Providers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static Monitel.Mal.Context.CIM16.Names;
using static System.Net.Mime.MediaTypeNames;


namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    internal class DiffImportProcessor : IDiffImportProcessor
    {
        private readonly IDiffImportProcessor _ckProcessor;
        private DifferenceModel _dmCim;
        private IModelImage _miCim;
        private BaseObjectRoot bor = null;
        private IPlatformLogger logger;

        // ======== Атрибуты и ассоциации с префиксом so для переноса ========
        private Dictionary<string, string[]> attributesWithSOextension = DataWithSOextension.AttributesWithSOextension;
        private Dictionary<string, string[]> assocsWithSOextension = DataWithSOextension.AssocsWithSOextension;
        // переменные для хранения данных
        private Dictionary<string[], Dictionary<Guid, object>> attributesWithSOextensionDATA;
        private Dictionary<string, List<ClassAssociation>> assocsWithSOextensionMetaData;
        private Dictionary<ClassAssociation, Dictionary<Guid, Guid[]>> assocsWithSOextensionDATA;

        // ======== Расчетные параметры ========
        private string[] calculatedParams = new string[] { "r", "x", "g", "b", "bPerSection", "gPerSection" };
        private string[] calculatedClassNames = new string[] { "ACLineSegment", "PowerTransformerEnd", "StaticVarCompensator", "SeriesCompensator", "LinearShuntCompensator" }; //названия классов расчетных параметров

        // ======== Другие переменные для хранения данных ========
        private Dictionary<Guid, double> bPerSections;
        private Dictionary<Guid, int> assetsPhase;

        // ======== Для проверки при переносе данных о кривых ========
        private List<Guid> blanckLimitUids = new List<Guid>();
        private List<string> listOfClassesToIgnoreInfoData = new List<string> { nameof(Breaker), nameof(Disconnector) };
        private Dictionary<string, Dictionary<Guid, string>> ignoredObjects;

        // ======== Для перемоделирования СЭС и ВЭС ========
        private ImportSolarWindStartegyFactory _strategyFactory;
        private List<(IConversionStrategyImport strategy, DifferenceObject foGost, DifferenceObject roGost)> strategyList_СЭС_ВЭС;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="ckRdfProvider"></param>
        public DiffImportProcessor(BaseRdfProvider ckRdfProvider)
        {
            _ckProcessor = ckRdfProvider.CreateDiffImportProcessor();
        }

        /// <summary>
        /// Инициализирует конвертер
        /// </summary>
        /// <param name="dmCim">Преобразованный набор изменений, который будет применен к модели</param>
        /// <param name="options">Параметры</param>
        /// <returns>Исходный набор изменений, который будет загружен из CIM XML</returns>
        public DifferenceModel InitProcess(DifferenceModel dmCim, DiffImportProcessorOptions options)
        {
            _dmCim = dmCim;
            _miCim = options.TargetModel;
            bor = _miCim.GetObjects<BaseObjectRoot>()[0];
            logger = options.Logger;

            // ======== Инициализируем переменные ========
            bPerSections = new Dictionary<Guid, double>();
            assetsPhase = new Dictionary<Guid, int>();
            attributesWithSOextensionDATA = new Dictionary<string[], Dictionary<Guid, object>>();
            assocsWithSOextensionMetaData = new Dictionary<string, List<ClassAssociation>>();
            assocsWithSOextensionDATA = new Dictionary<ClassAssociation, Dictionary<Guid, Guid[]>>();
            ignoredObjects = new Dictionary<string, Dictionary<Guid, string>>();

            _strategyFactory = new ImportSolarWindStartegyFactory(_miCim);
            strategyList_СЭС_ВЭС = new List<(IConversionStrategyImport strategy, DifferenceObject foGost, DifferenceObject roGost)>();

            // Создание переменных для ассоциаций
            foreach (var assocPair in assocsWithSOextension)
            {
                foreach (var assocName in assocPair.Value)
                {
                    ClassAssociation assoc = _miCim.MetaData.Classes[assocPair.Key].Associations.FirstOrDefault(p => p.Name == assocName);
                    if (!assocsWithSOextensionMetaData.ContainsKey(assocPair.Key))
                        assocsWithSOextensionMetaData.Add(assocPair.Key, new List<ClassAssociation> { assoc });
                    else
                        assocsWithSOextensionMetaData[assocPair.Key].Add(assoc);
                }
            }

            return _ckProcessor.InitProcess(dmCim, options);
        }



        /// <summary>
        /// Проверяет нужен ли копировать объект в преобразованный набор изменений общим алгоритмом
        /// </summary>
        /// <param name="foGost">Объект в forward-секции исходного набора изменений</param>
        /// <param name="roGost">Объект в reverse-секции исходного набора изменений</param>
        /// <param name="mcCim">Класс объекта в преобразованном наборе изменений</param>
        /// <returns>True если объект должен быть скопирован в преобразованный набор изменений</returns>
        public bool CheckObject(DifferenceObject foGost, DifferenceObject roGost, out MetaClass mcCim)
        {
            try
            {
                var classEntity = (foGost ?? roGost)?.ObjectClass;
                var props = (foGost ?? roGost)?.Properties;
                var className = classEntity?.Name ?? "";
                var foClassName = foGost?.ObjectClass?.Name;

                // Импорт СЭС и ВЭС
                IConversionStrategyImport currentObjectStrategy = _strategyFactory.GetStrategy(foGost, roGost);
                if (currentObjectStrategy != null)
                {
                    strategyList_СЭС_ВЭС.Add((currentObjectStrategy, foGost, roGost));
                    mcCim = null;
                    return false;
                }

                // Для тестов не вызываем базовый импорт, чтобы не появлялись TestME, а сразу возвращаем True если в модели есть класс
                if (className == nameof(NoLoadTest) || className == nameof(ShortCircuitTest))
                    return _dmCim.MetaData.Classes.TryGetMetaEntity(className, out mcCim);

                // Запоминаем Limit, если у него не задано value
                if (foGost?.ObjectClass?.Parent?.Name == "OperationalLimit" && !props.Any(x => x?.Name == "value") && !props.Any(x => x?.Name == "normalValue"))
                    blanckLimitUids.Add(foGost.ObjectUid);

                // Задание параметров RatioTapChangerTablePoint по умолчанию
                if (foGost?.ObjectClass?.Name == nameof(RatioTapChangerTablePoint))
                {
                    _dmCim.MetaData.Classes.TryGetMetaEntity(className, out mcCim);
                    var array = new string[] { "x", "g", "b", "r" };
                    foreach (var obj in array)
                    {
                        if (!props.Any(x => x.Name == obj))
                        {
                            mcCim.TryGetAttribute(obj, out var attr);
                            foGost.AddInt32(attr, 0);
                        }
                    }
                }

                // ======== Сохраняем данные ассоциаций с префиксом so по метаданным ========
                if (assocsWithSOextensionMetaData.ContainsKey(className)) //есть ли рассматриваемый класс в assocsWithSOextension
                {
                    foreach (var assoc in assocsWithSOextensionMetaData[className])
                    {
                        //проверяем, задана ли ассоциация
                        Guid[] assocValues = null;
                        if (assoc.Kind == PropertyKind.AssocToOne && foGost?.Properties.FirstOrDefault(x => x.Name == assoc.Name) != null)
                            assocValues = new Guid[] { foGost.GetToOneUid(assoc) };
                        else if (assoc.Kind == PropertyKind.AssocToMany && foGost?.Properties.FirstOrDefault(x => x.Name == assoc.Name) != null)
                            assocValues = foGost.GetToManyUids(assoc).ToArray();

                        //если у ассоциации есть значение, то сохраняем
                        if (assocValues.FirstOrDefault() != null)
                        {
                            if (!assocsWithSOextensionDATA.ContainsKey(assoc))
                                assocsWithSOextensionDATA.Add(assoc, new Dictionary<Guid, Guid[]>());
                            assocsWithSOextensionDATA[assoc].Add(foGost.ObjectUid, assocValues);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Write(LogCategory.Error, LogPriority.High, "Check object: " + e.Message + " " + (foGost ?? roGost)?.ObjectUid.ToString());
            }

            var res = _ckProcessor.CheckObject(foGost, roGost, out mcCim);
            return res;
        }


        /// <summary>
        /// Проверяет нужно ли копировать значение свойства в преобразованный набор изменений общим алгоритмом
        /// </summary>
        /// <param name="foGost">Объект в forward-секции исходного набора изменений</param>
        /// <param name="roGost">Объект в reverse-секции исходного набора изменений</param>
        /// <param name="cpGost">Свойство в исходном наборе изменений</param>
        /// <param name="foCim">Объект в forward-секции преобразованного набора изменений</param>
        /// <param name="roCim">Объект в reverse-секции преобразованного набора изменений</param>
        /// <param name="cpCim">Свойство объекта в преобразованном наборе изменений</param>
        /// <returns>True если значение свойства должно быть скопировано в преобразованный набор изменений</returns>
        public bool CheckProperty(DifferenceObject foGost, DifferenceObject roGost, ClassProperty cpGost, DifferenceObject foCim, DifferenceObject roCim, out ClassProperty cpCim)
        {
            try
            {
                var className_foCim = foCim?.ObjectClass?.Name;
                var propName = cpGost?.Name;

                if (cpGost?.Kind == PropertyKind.Attribute)
                {

                    // ======== Проверка корректности числовых данных ========
                    if (cpGost.StoredType == PrimitiveType.Float32)
                    {
                        try
                        {
                            if (!IsCorrectNumber(foGost?.GetFloat32(cpGost as ClassAttribute)))
                            {
                                cpCim = null;
                                return false;
                            }
                        }
                        catch { }
                    }
                    if (cpGost.StoredType == PrimitiveType.Float64)
                    {
                        try
                        {
                            if (!IsCorrectNumber(foGost?.GetFloat64(cpGost as ClassAttribute)))
                            {
                                cpCim = null;
                                return false;
                            }
                        }
                        catch { }
                    }

                    // ======== LinearShuntCompensator ========
                    if (className_foCim == "LinearShuntCompensator")
                    {
                        if (propName == "nomU") // не переносим
                        {
                            cpCim = null;
                            return false;
                        }
                        if (propName == "bPerSection") // не переносим
                        {
                            if (foGost != null)
                                bPerSections.Add(foGost.ObjectUid, foGost.GetFloat32(cpGost as ClassAttribute)); // сохраняем
                            cpCim = null;
                            return false;
                        }
                    }

                    // ======== Asset ========
                    if (className_foCim == "Asset")
                    {
                        if (propName == "phase")
                        {
                            int id = foGost.GetEnum(cpGost as ClassAttribute);
                            assetsPhase.Add(foGost.ObjectUid, id); // сохраняем
                        }
                        if (propName == "phases") // не переносим
                        {
                            cpCim = null;
                            return false;
                        }
                        if (propName == "lifecycleDate") // не переносим
                        {
                            cpCim = null;
                            return false;
                        }
                    }

                    // ======== Игнорирование свойств, которые должны переноситься из AssetInfo ========
                    if (listOfClassesToIgnoreInfoData.Contains(className_foCim))
                    {
                        if (propName == "breakingCapacity" || propName == "ratedCurrent") // не переносим
                        {
                            cpCim = null;
                            return false;
                        }
                    }

                    // ======== Сохраняем данные атрибутов с префиксом so ========
                    if (attributesWithSOextension.ContainsKey(className_foCim)) // есть ли рассматриваемый класс в attributesWithSOextension
                    {
                        if (attributesWithSOextension[className_foCim].Any(x => x == propName)) // есть ли рассматриваемый атрибут в attributesWithSOextension у класса выше
                        {
                            string[] key = new string[] { className_foCim, propName };
                            if (!attributesWithSOextensionDATA.ContainsKey(key)) // есть ли в словаре значений подсловарь для данного атрибута
                                attributesWithSOextensionDATA.Add(key, new Dictionary<Guid, object>());

                            //сохраняем значение
                            if (cpGost.StoredType == PrimitiveType.Float64)
                                attributesWithSOextensionDATA[key].Add(foGost.ObjectUid, foGost.GetFloat64(cpGost as ClassAttribute));
                            else if (cpGost.StoredType == PrimitiveType.Bit)
                                attributesWithSOextensionDATA[key].Add(foGost.ObjectUid, foGost.GetBool(cpGost as ClassAttribute));
                        }
                    }

                    // ======== Проверка необходимости игнорирования расчетных параметров ========
                    if (IsIgnoringCalculatedParams(foCim, roCim, cpGost))
                    {
                        cpCim = null;
                        return false;
                    }
                }
                return _ckProcessor.CheckProperty(foGost, roGost, cpGost, foCim, roCim, out cpCim);
            }
            catch (Exception e)
            {
                logger.Write(LogCategory.Error, LogPriority.High, "Check property: " + e.Message + " " + (foGost ?? roGost)?.ObjectUid.ToString());
                cpCim = null;
                return false;
            }
        }


        /// <summary>
        /// Проверка корректности числового значения
        /// </summary>
        private bool IsCorrectNumber(double? d)
        {
            if (d == null)
                return true;
            if (double.IsNaN((double)d))
                return false;
            if (double.IsInfinity((double)d))
                return false;
            if (double.IsNegativeInfinity((double)d))
                return false;
            return true;
        }


        /// <summary>
        /// Проверка необходимости игнорирования расчетных параметров
        /// </summary>
        private bool IsIgnoringCalculatedParams(DifferenceObject foCim, DifferenceObject roCim, ClassProperty cpGost)
        {
            if ((foCim?.ObjectClass?.Name == nameof(SeriesCompensator) || roCim?.ObjectClass?.Name == nameof(SeriesCompensator)) && cpGost?.Name == "x")
                return false;
            if ((calculatedClassNames.Contains(foCim?.ObjectClass?.Name) || calculatedClassNames.Contains(roCim?.ObjectClass?.Name)) && calculatedParams.Contains(cpGost?.Name))
                return true;
            return false;
        }


        /// <summary>
        /// Проверяет должна ли ссылка на объект быть скопирована в преобразованный набор изменений
        /// </summary>
        /// <param name="dsGost">Секция исходного набора изменений</param>
        /// <param name="uid">Идентификатор объекта</param>
        /// <param name="cpCim">Ассоциация в преобразованном наборе изменений</param>
        /// <returns>True если ссылка на объект должна быть скопирована в преобразованный набор изменений</returns>
        public bool CheckLinkedUid(DifferenceSet dsGost, Guid uid, ClassAssociation cpCim)
        {
            try
            {
                return _ckProcessor.CheckLinkedUid(dsGost, uid, cpCim);
            }
            catch (Exception e)
            {
                logger.Write(LogCategory.Error, LogPriority.High, "Check linked UID: " + e.Message + " " + uid.ToString());
                return false;
            }
        }


        /// <summary>
        /// Завершает процесс перобразования
        /// </summary>
        public void EndProcess()
        {
            // Импорт СЭС и ВЭС
            foreach (var elem in strategyList_СЭС_ВЭС)
                elem.strategy.ImportElement(elem.foGost, elem.roGost);

            GenerateTextProtocol_CurrentLimit_CurrentVsTapStepLimitCurve();

            _ckProcessor.EndProcess();

            GeneralProcessing();
            DeleteSomeData_PowerTransformer();
            SetXForReactors();

            // ======== Перенос данных ========
            CopyData_inUseDate_To_lifecycleDate();
            CopyData_SwitchInfo_To_Switch();
            CopyData_phases_To_phase();
            CopyData_normalValue_To_value();

            Process_SoProps();
            TemporaryStub();

            CreateProtocolForm();
        }


        /// <summary>
        /// -
        /// </summary>
        private HashSet<IdentifiedObject> GetChildObjectsOfTransformerTest(IdentifiedObject obj)
        {
            HashSet<IdentifiedObject> listToReturn = new HashSet<IdentifiedObject>();
            listToReturn.UnionWith(obj.ChildObjects.OfType<TransformerTest>());
            listToReturn.UnionWith(obj.ChildObjects.OfType<PowerTransformerInfo>());
            foreach (var c in obj.ChildObjects)
            {
                listToReturn.UnionWith(GetChildObjectsOfTransformerTest(c));
            }
            return listToReturn;
        }


        /// <summary>
        /// -
        /// </summary>
        private void SetXForReactors()
        {
            foreach (var v in bPerSections)
            {
                var obj = _miCim.GetObject<LinearShuntCompensator>(v.Key)?.Assets.FirstOrDefault().AssetInfo as ShuntReactorInfo;
                if (obj != null)
                {
                    obj.SetAttribute("x", Math.Round(1 / v.Value, 2));
                }
            }
            bPerSections.Clear();
        }


        /// Формирование строки вывода с информацией об игнорируемых объектах CurrentLimit и CurrentVsTapStepLimitCurve
        /// </summary>
        private void GenerateTextProtocol_CurrentLimit_CurrentVsTapStepLimitCurve()
        {
            var curLims = _miCim.GetObjects<CurrentLimit>();
            //игнорируем таблицы, которые подключаются к Limit без value
            List<DifferenceObject> ignoredCurves = new List<DifferenceObject>();
            foreach (var o in _dmCim.Forward.All.Where(x => x.ObjectClass?.Name == "CurrentVsTapStepLimitCurve" && !x.IsDescription))
            {
                if (o.Properties.Where(x => x?.Name == "CurrentLimits").Count() > 0) //проверка на случай если добавляется Curve без привязки к пределу или изм существующий Curve, которого по факту нет в модели
                {
                    Guid limitUid = o.GetToManyUids(o.Properties.Where(x => x?.Name == "CurrentLimits").FirstOrDefault() as ClassAssociation).FirstOrDefault();
                    if (blanckLimitUids.Any(y => y == limitUid))
                    {
                        ignoredCurves.Add(o);
                        string reason = "Объект привязывается к CurrentLimit без value:";
                        if (ignoredObjects.ContainsKey(reason))
                            ignoredObjects[reason].Add(o.ObjectUid, o.ObjectClass.Name.Replace("CurrentVsTapStepLimitCurve", "TapChangerDependentLimitTable"));
                        else
                        {
                            ignoredObjects.Add(reason, new Dictionary<Guid, string>() {
                            {
                                o.ObjectUid,
                                o.ObjectClass.Name.Replace("CurrentVsTapStepLimitCurve", "TapChangerDependentLimitTable")
                            }});
                        }
                    }
                }
                else //если добавляется curve без привязки к пределу (значит, что либо действительно такой кривой curve, либо он связывается с пределом, которого нет в модели, поэтому связь с пределом не попадает в применяемый набор изменений
                {
                    ignoredCurves.Add(o);
                    string reason = "У объекта нет связи с CurrentLimit:";
                    if (ignoredObjects.ContainsKey(reason))
                        ignoredObjects[reason].Add(o.ObjectUid, o.ObjectClass.Name.Replace("CurrentVsTapStepLimitCurve", "TapChangerDependentLimitTable"));
                    else
                    {
                        ignoredObjects.Add(reason, new Dictionary<Guid, string>() {
                            {
                                o.ObjectUid,
                                o.ObjectClass.Name.Replace("CurrentVsTapStepLimitCurve", "TapChangerDependentLimitTable")
                            }});
                    }
                }
            }
            foreach (var o in ignoredCurves)
            {
                _dmCim.Forward.Delete(o);
            }
        }


        /// <summary>
        /// Удаляет TransformerMeshImpedance, TransformerTest, PowerTransformerInfo
        /// </summary>
        private void DeleteSomeData_PowerTransformer()
        {
            var removedItemsUids = _dmCim.Reverse.All.Where(x => !x.IsDescription).Select(x => x.ObjectUid).Except(_dmCim.Forward.All.Where(x => !x.IsDescription).Select(x => x.ObjectUid));
            HashSet<IdentifiedObject> removeHS = new HashSet<IdentifiedObject>();
            foreach (var u in removedItemsUids)
            {
                var obj = _miCim.GetObject(u);
                if (obj is PowerTransformer pt)
                {
                    foreach (var e in pt.PowerTransformerEnd)
                    {
                        removeHS.UnionWith(e.FromMeshImpedance);
                        removeHS.UnionWith(e.ToMeshImpedance);
                    }
                    removeHS.UnionWith(GetChildObjectsOfTransformerTest(pt));
                }
            }
            _miCim.RemoveObjects(removeHS);
        }


        /// <summary>
        /// Копирование inUseDate в lifecycleDate
        /// </summary>
        private void CopyData_inUseDate_To_lifecycleDate()
        {
            var prop_lifeCycleDate = _miCim.MetaData.Classes.First(x => x.Name == nameof(Asset)).AllProperties.First(x => x.Name == "lifecycleDate") as ClassAttribute;
            var prop_inUseDate = _miCim.MetaData.Classes.First(x => x.Name == nameof(Asset)).AllProperties.First(x => x.Name == "inUseDate") as ClassAttribute;
            foreach (var o in _dmCim.Forward.All.Where(x => x.Properties.Any(y => y.Name == "inUseDate")))
            {
                var obj = _miCim.GetObject(o.ObjectUid) as Asset;
                if (!o.IsNull(prop_inUseDate))
                {
                    if (obj != null)
                    {
                        var inUseValue = SOFullImportRule.ExtractInUseDate(o.GetString(prop_inUseDate));
                        var initialInServiceValue = SOFullImportRule.ExtractInitialInServiceDate(obj?.lifecycleDate ?? "");
                        if (inUseValue != initialInServiceValue)
                            SOFullImportRule.CopyToLCD(o.GetString(prop_inUseDate), obj?.lifecycleDate ?? "", o, prop_lifeCycleDate);
                    }
                    else
                        SOFullImportRule.CopyToLCD(o.GetString(prop_inUseDate), "", o, prop_lifeCycleDate);
                }
                else
                    SOFullImportRule.CopyToLCD("", "", o, prop_lifeCycleDate);
            }
        }


        /// <summary>
        /// Копирование breakingCapacity и ratedCurrent
        /// </summary>
        private void CopyData_SwitchInfo_To_Switch()
        {
            var prop_breakingCapacity_Info = _miCim.MetaData.Classes.First(x => x.Name == nameof(SwitchInfo)).AllProperties.First(x => x.Name == "breakingCapacity") as ClassAttribute;
            var prop_ratedCurrent_Info = _miCim.MetaData.Classes.First(x => x.Name == nameof(SwitchInfo)).AllProperties.First(x => x.Name == "ratedCurrent") as ClassAttribute;
            var prop_breakingCapacity_Breaker = _miCim.MetaData.Classes.First(x => x.Name == nameof(Breaker)).AllProperties.First(x => x.Name == "breakingCapacity") as ClassAttribute;
            var prop_ratedCurrent_Switch = _miCim.MetaData.Classes.First(x => x.Name == nameof(Switch)).AllProperties.First(x => x.Name == "ratedCurrent") as ClassAttribute;

            var listOfClassesInfo = new List<string> { nameof(BreakerInfo), nameof(SwitchInfo) };
            var list = _dmCim.Forward.All
                .Where(x => x.Properties.Any(y => y.Name == "ratedCurrent" || y.Name == "breakingCapacity") &&
                    listOfClassesInfo.Contains(x.ObjectClass.Name))
                .ToList();
            foreach (var o in list)
            {
                // Если оба свойства удалены, то ничего не переносим
                if (o.Properties.Any(y => y == prop_ratedCurrent_Info) &&
                    o.Properties.Any(y => y == prop_breakingCapacity_Info) &&
                    o.IsNull(prop_ratedCurrent_Info) && o.IsNull(prop_breakingCapacity_Info))
                    continue;

                var obj = _miCim.GetObject(o.ObjectUid) as SwitchInfo;

                // Если дифф Info, то переносим в Switch в модели
                if (obj != null)
                {
                    foreach (var asset in obj.Assets)
                    {
                        foreach (var psr in asset.PowerSystemResources)
                        {
                            if (o.Properties.Any(y => y == prop_breakingCapacity_Info) && !o.IsNull(prop_breakingCapacity_Info) && psr is Breaker b)
                                b.breakingCapacity = o.GetFloat64(prop_breakingCapacity_Info);
                            if (o.Properties.Any(y => y == prop_ratedCurrent_Info) && !o.IsNull(prop_ratedCurrent_Info) && psr is Switch s)
                                s.ratedCurrent = o.GetFloat64(prop_ratedCurrent_Info);
                        }
                    }
                }
                else
                {
                    // Если создается и Info и Switch, то надо в Switch добавить свойство (предварительно исключаем его перенос)
                    if (o.Properties.Any(y => y.Name == "Assets"))
                    {
                        var assetUids = o.GetToManyUids(o.Properties.First(y => y.Name == "Assets") as ClassAssociation).ToList();
                        if (assetUids.Count() == 0)
                            continue;

                        foreach (var ass in _dmCim.Forward.All.Where(x => assetUids.Contains(x.ObjectUid)))
                        {
                            if (ass.Properties.Any(y => y.Name == "PowerSystemResources"))
                            {
                                var psrUids = ass.GetToManyUids(ass.Properties.First(y => y.Name == "PowerSystemResources") as ClassAssociation).ToList();
                                if (psrUids.Count() == 0)
                                    continue;

                                foreach (var psr in _dmCim.Forward.All.Where(x => psrUids.Contains(x.ObjectUid)))
                                {
                                    if (!psr.IsDescription)
                                    {
                                        if (psr.ObjectClass.Name == nameof(Breaker))
                                        {
                                            if (o.Properties.Any(y => y == prop_breakingCapacity_Info))
                                                psr.AddFloat64(prop_breakingCapacity_Breaker, o.GetFloat64(prop_breakingCapacity_Info));
                                            if (o.Properties.Any(y => y == prop_ratedCurrent_Info))
                                                psr.AddFloat64(prop_ratedCurrent_Switch, o.GetFloat64(prop_ratedCurrent_Info));
                                        }
                                        else
                                        {
                                            if (o.Properties.Any(y => y == prop_ratedCurrent_Info))
                                                psr.AddFloat64(prop_ratedCurrent_Switch, o.GetFloat64(prop_ratedCurrent_Info));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        

        /// <summary>
        /// Копирование normalValue и value
        /// </summary>
        private void CopyData_normalValue_To_value()
        {
            var list = _dmCim.Forward.All.Union(_dmCim.Reverse.All)
                .Where(x => 
                    (x.ObjectClass?.Name == "CurrentLimit" || 
                    x.ObjectClass?.Name == "VoltageLimit" || 
                    x.ObjectClass?.Name == "FrequencyeLimit")  && 
                    x.Properties.Any(p => p.Name == "normalValue"))
                .ToList();

            foreach (var obj in list)
            {              
                var className = obj.ObjectClass.Name;

                var prop_value = _miCim.MetaData.Classes.First(x => x.Name == className).AllProperties.First(x => x.Name == "value") as ClassAttribute;
                if (obj.Properties.Any(p => p.Name == "value"))
                    obj.RemoveEntire(prop_value);

                try
                {
                    var normalValue = obj.GetFloat64(obj.Properties.First(p => p.Name == "normalValue") as ClassAttribute);
                    obj.AddFloat64(prop_value, normalValue);
                }
                catch (Exception ex)
                {
                    obj.AddFloat64(prop_value, float.MinValue);
                }
            }
        }


        /// <summary>
        /// Заносит в модель все атрибуты и ассоциации СО
        /// </summary>
        private void Process_SoProps()
        {
            List<ClassAttribute> attrs = new List<ClassAttribute>();
            foreach (var elem in attributesWithSOextensionDATA)
            {
                attrs.Add(_dmCim.MetaData.Classes[elem.Key[0]].Attributes.FirstOrDefault(x => x.Name == elem.Key[1]));
            }

            foreach (var o in _dmCim.Forward.All) // изм объекты
            {
                //запись атрибутов
                if (attributesWithSOextensionDATA.Any(x => x.Value.ContainsKey(o.ObjectUid)))
                {
                    //выбираем только записи с нужным Uid

                    //например, из этого для guid1
                    //{
                    //    { ["1", "2"], { { guid1: val1 }, { guid2: val2 } } },
                    //    { ["3", "4"], { { guid3: val3 }, { guid1: val4 } } }
                    //}

                    //получаем
                    //{
                    //    { ["1", "2"], { { guid1: val1 } } },
                    //    { ["3", "4"], { { guid1: val4 } } }
                    //}
                    var neededAttrInf = attributesWithSOextensionDATA.Where(x => x.Value.ContainsKey(o.ObjectUid))
                                                                     .ToDictionary(x => (string[])x.Key.Clone(), // копируем ключ
                                                                                   x => new Dictionary<Guid, object> { { o.ObjectUid, x.Value[o.ObjectUid] } }); // только нужный Guid
                    foreach (var elem in neededAttrInf)
                    {
                        var neededAttr = attrs.FirstOrDefault(x => x.Name == elem.Key[1]);
                        var neededAttrValue = elem.Value.Values.FirstOrDefault();
                        if (neededAttr.StoredType == PrimitiveType.Float32)
                            o.AddFloat64(neededAttr, (float)neededAttrValue);
                        else if (neededAttr.StoredType == PrimitiveType.Bit)
                            o.AddBool(neededAttr, (bool)neededAttrValue);
                    }
                }

                //запись ассоциаций
                if (assocsWithSOextensionDATA.Any(x => x.Value.ContainsKey(o.ObjectUid)))
                {
                    var neededAssocInf = assocsWithSOextensionDATA.Where(x => x.Value.ContainsKey(o.ObjectUid))
                                                                  .ToDictionary(x => x.Key,
                                                                                x => new Dictionary<Guid, Guid[]> { { o.ObjectUid, x.Value[o.ObjectUid] } });
                    foreach (var elem in neededAssocInf)
                    {
                        var neededAssoc = elem.Key;
                        var neededAssocValue = elem.Value.Values.FirstOrDefault();
                        if (neededAssoc.Kind == PropertyKind.AssocToOne)
                            o.AddToOneUid(neededAssoc, neededAssocValue.FirstOrDefault());
                        else if (neededAssoc.Kind == PropertyKind.AssocToMany)
                            o.AddToManyUids(neededAssoc, neededAssocValue);
                    }
                }
            }
        }


        /// <summary>
        /// -
        /// </summary>
        private void GeneralProcessing()
        {
            var casParentObject = _dmCim.MetaData.Classes[nameof(IdentifiedObject)].Associations.FirstOrDefault(p => p.Name == nameof(IdentifiedObject.ParentObject));
            var casName = _dmCim.MetaData.Classes[nameof(IdentifiedObject)].Attributes.FirstOrDefault(x => x.Name == nameof(IdentifiedObject.name));
            var borGuid = new Guid("00000001-0000-0000-C000-0000006D746C");
            foreach (var o in _dmCim.Forward.All.Where(p => !p.IsDescription).Where(x => !_dmCim.Reverse.All.Select(y => y.ObjectUid).Contains(x.ObjectUid))) // добавленные объекты
            {
                if (o.ObjectClass.IsDescendantOf(_dmCim.MetaData.Classes[nameof(IdentifiedObject)]) &&
                    o.ObjectClass != _dmCim.MetaData.Classes[nameof(Terminal)] &&
                    o.ObjectClass != _dmCim.MetaData.Classes[nameof(ConnectivityNode)])
                {
                    //связываем добавленный объект с корнем
                    if (_miCim.GetObject(o.ObjectUid) == null)
                        o.AddToOneUid(casParentObject, borGuid);

                    // добавляем имя, если не задано
                    if (!o.Properties.Contains(casName))
                    {
                        if (_miCim.GetObject(o.ObjectUid) == null)
                            o.AddString(casName, $"{o?.ObjectClass?.DisplayName ?? "New object"}: {o.ObjectUid}");
                    }
                }
            }
        }


        /// <summary>
        /// Перенос значений фаз
        /// </summary>
        private void CopyData_phases_To_phase()
        {
            var attrPhase = _dmCim.MetaData.Classes[nameof(Asset)].Attributes.FirstOrDefault(x => x.Name == nameof(Asset.phase));
            foreach (var o in _dmCim.Forward.All.Where(p => !p.IsDescription).Where(x => !_dmCim.Reverse.All.Select(y => y.ObjectUid).Contains(x.ObjectUid)))
            {
                //добавляем phase
                if (o.ObjectClass?.Name == nameof(Asset) && assetsPhase.ContainsKey(o.ObjectUid))
                {
                    o.AddEnum(attrPhase, assetsPhase[o.ObjectUid]);
                }
            }
        }


        /// <summary>
        /// Заглушка из-за ошибки монитора (при импорте новых кривых всегда создаются точки)
        /// </summary>
        private void TemporaryStub()
        {
            var pointsToDelete = _dmCim.Forward.All.Where(p => !p.IsDescription && p.ObjectClass?.Name == nameof(TapChangerDependentLimitPoint)).ToList();
            foreach (var o in pointsToDelete)
            {
                _dmCim.Forward.Delete(o);
            }
        }


        /// <summary>
        /// Создает форму с информацией об игнорируемых объектах
        /// </summary>
        private void CreateProtocolForm()
        {
            if (ignoredObjects.Count() > 0)
            {
                string text = "======== ДЛЯ ЗАВЕРШЕНИЯ ИМПОРТА ЗАКРОЙТЕ ДАННОЕ ОКНО ========" + Environment.NewLine + Environment.NewLine;

                foreach (var elem in ignoredObjects)
                {
                    text += "Причина игнорирования: " + elem.Key + Environment.NewLine;
                    foreach (var elem1 in elem.Value)
                    {
                        text += elem1.Value + ": " + elem1.Key.ToString() + Environment.NewLine;
                    }
                    text += Environment.NewLine;
                }
#if !WITHOUT_FORM
                Forms.CreateFormIgnoredObj(text);
#endif
            }
        }
    }
}
