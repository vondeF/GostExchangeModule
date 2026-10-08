using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Snapshot;
using Monitel.Protocol.Common;
using Monitel.Serialization.CIMXML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Meta;
using Newtonsoft.Json.Linq;
using static Monitel.Mal.Context.CIM16.Names;
using Monitel.PlatformInfrastructure;
using Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    internal class GOSTImportProvider : IImportProvider
    {
        private IModelImage ModelImage;
        private IMalObject lastObject;
        private IFormatProvider formatProvider = new CultureInfo("en-US");
        private readonly IImportProvider _ckProvider;

        // ======== Для перемоделирования СЭС и ВЭС ========
        private ImportSolarWindStartegyFactory _strategyFactory;
        private IConversionStrategyImport _currentObjectStrategy;

        // ======== Расчетные параметры ========
        private string[] calculatedParams = new string[] { "r", "x", "g", "b", "bPerSection", "gPerSection", "gch", "bch" };
        private string[] arrayParams_RatioTapChangerTablePoint = new string[] { "x", "g", "b", "r" };

        // ======== Переменные для хранения данных ========
        private Dictionary<Guid, double> bPerSections;

        // ======== Другое ========
        private Dictionary<Guid, IMalObject> importedObjects;
        private HashSet<Guid> duples;
        

        public GOSTImportProvider(BaseRdfProvider ckRdfProvider)
        {
            _ckProvider = ckRdfProvider.CreateImportProvider();
        }


        /// <summary>
        /// Принудительно учитывать значение поля mRid при его наличии
        /// </summary>
        public bool ForceUseMrid => _ckProvider.ForceUseMrid;


        /// <summary>
        /// Проверяет класс импортиремого объекта
        /// </summary>
        /// <param name="options">Параметры</param>
        /// <returns>Можно ли продолжать импорт</returns>
        public bool StartParsingDocument(ImportProviderOptions options)
        {
            ModelImage = options.ModelImage;

            // ======== Инициализируем переменные ========
            bPerSections = new Dictionary<Guid, double>();

            _strategyFactory = new ImportSolarWindStartegyFactory(ModelImage);
            importedObjects = new Dictionary<Guid, IMalObject>();
            duples = new HashSet<Guid>();

            return _ckProvider.StartParsingDocument(options);
        }


        /// <summary>
        /// Проверяет класс импортиремого объекта
        /// </summary>
        /// <param name="element">Разбираемый элемент</param>
        /// <param name="objectClass">Класс модели, экземпляр которого будет создан</param>
        /// <returns>true если требуется импортировать объект общим алгоритмом, false если не требуется, null если провайдер не знает про объект</returns>
        public bool? CheckClass(XElement element, ref MetaClass objectClass)
        {
            var guid = new Guid(element.FirstAttribute.Value.Substring(2));
            if (importedObjects.ContainsKey(guid))
            {
                duples.Add(guid); //uid импортированных на данный момент объектов
                return null;
            }

            // Для тестов не вызываем базовый импорт, чтобы не появлялись TestME, а сразу говорим true
            if (objectClass?.Name == nameof(NoLoadTest) || objectClass?.Name == nameof(ShortCircuitTest))
                return true;

            // Импорт СЭС и ВЭС
            _currentObjectStrategy = _strategyFactory.GetStrategy(objectClass?.Name, element);
            if (_currentObjectStrategy != null)
            {
                _currentObjectStrategy.ImportElement(element);
                return false;
            }

            return _ckProvider.CheckClass(element, ref objectClass);
        }
        


        /// <summary>
        /// Начинает разбор элемента-объекта
        /// </summary>
        /// <param name="element">Разбираемый элемент</param>
        /// <param name="malObject">Объект</param>
        public void StartParsingObject(XElement element, IMalObject malObject) //импортируем element из XML в malObject в модели
        {
            var uid = new Guid(element.FirstAttribute.Value.Substring(2));
            if (!duples.Contains(uid)) //объект с таким uid еще не импортирован
            {
                importedObjects.Add(uid, malObject); //запись в список импортированных объектов
                _ckProvider.StartParsingObject(element, malObject); //импорт объекта
            }
        }
     

        /// <summary>
        /// Начинает разбор элемента-свойства
        /// </summary>
        /// <param name="element">Разбираемый элемент</param>
        /// <param name="malObject">Объект для которого происходит разбор</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="property">Свойство</param>
        /// <returns>true если требуется импортировать свойство общим алгоритмом, false если не требуется, null если провайдер не знает про свойство</returns>
        public bool? StartParsingProperty(XElement element, IMalObject malObject, string propertyName, ref ClassProperty property)
        {
            if (!duples.Contains(malObject.Uid)) //импортированный объект проверяем еще раз, что он не дублируется
            {
                if (malObject is WireInfo && propertyName == "sizeDescription")
                    return true;
                if (malObject is RotatingMachine && propertyName == "ReserveExcitationSystem")
                    return true;
                if (malObject is ExcitationSystem && propertyName == "RotatingMachines")
                    return true;
                if (malObject is LinearShuntCompensator && propertyName == "nomU")
                    return false;
                if (IsIgnoringCalculatedParams(malObject, propertyName))
                    return false;
                if (malObject is Asset && propertyName == "phases")
                    return false;
                if (malObject is RatioTapChangerTablePoint && arrayParams_RatioTapChangerTablePoint.Contains(propertyName))
                    malObject.SetInt(property, 0);

                if (property != null)
                {
                    if (property.StoredType == PrimitiveType.Float32 || property.StoredType == PrimitiveType.Float64)
                    {
                        if (element.Value == "NaN")
                            return false; //не обрабатываем, если тип свойства Float32 или Float64 и при этом значение свойства NaN (отсутсвует число)
                    }
                }
                return _ckProvider.StartParsingProperty(element, malObject, propertyName, ref property);
            }
            else
                return null;
        }


        /// <summary>
        /// Проверка необходимости игнорирования расчетных параметров
        /// </summary>
        private bool IsIgnoringCalculatedParams(IMalObject obj, string propertyName)
        {
            if ((obj is ACLineSegment || obj is PowerTransformerEnd || obj is StaticVarCompensator || obj is LinearShuntCompensator)
                && calculatedParams.Contains(propertyName))
                return true;
            if (obj is SeriesCompensator && calculatedParams.Where(x => x != "x").Contains(propertyName))
                return true;
            return false;
        }


        /// <summary>
        /// Вызывается когда завершён разбор объекта
        /// </summary>
        /// <param name="malObject"></param>
        public void EndParsingObject(IMalObject malObject)
        {
            if (!duples.Contains(malObject.Uid)) //объект с таким uid еще не импортирован
            {
                lastObject = malObject;
                _ckProvider.EndParsingObject(malObject);
            }
        }


        /// <summary>
        /// Вызывается когда завершён разбор документа
        /// </summary>
        public void EndParsingDocument()
        {
            foreach (var obj in importedObjects.Values.OfType<IdentifiedObject>().Where(x => !(x is Terminal) && !(x is ConnectivityNode)).Where(x => string.IsNullOrEmpty(x.name)))
                obj.name = $"{obj.MetaType.Name}:{obj.Id}";

            ProcessDuplicates();
            //SetXForReactors();

            // ======== Перенос данных ========
            CopyData_phases_To_phase();
            CopyData_inUseDate_To_lifecycleDate();
            CopyData_SwitchInfo_To_Switch();
            CopyData_normalValue_To_value();

            ModelImage.RemoveObjects(ModelImage.GetObjects<TapChangerDependentLimitPoint>()); // заглушка из-за ошибки монитора (при импорте новых кривых всегда создаются точки)

            CreateProtocolForm();

            _ckProvider.EndParsingDocument();
        }


        /// <summary>
        /// Дубликаты переносятся в отдельную папку в корне
        /// </summary>
        private void ProcessDuplicates()
        {
            if (duples.Any())
            {
                var parent = ModelImage.CreateObject<Folder>();
                parent.ParentObject = ModelImage.GetObjects<BaseObjectRoot>().First();
                parent.name = "!!Задублированные идентификаторы";
                foreach (var g in duples)
                {
                    var c = ModelImage.CreateObject<Folder>();
                    c.name = g.ToString();
                    c.ParentObject = parent;
                }
            }
        }


        /// <summary>
        /// Копирование inUseDate в lifecycleDate
        /// </summary>
        private void CopyData_inUseDate_To_lifecycleDate()
        {
            foreach (var asset in importedObjects.Values.OfType<Asset>().Where(x => !string.IsNullOrEmpty(x.inUseDate)))
            {
                var inUseValue = SOFullImportRule.ExtractInUseDate(asset.inUseDate);
                var initialInServiceValue = SOFullImportRule.ExtractInitialInServiceDate(asset.lifecycleDate);

                if (asset != null && inUseValue != initialInServiceValue)
                    SOFullImportRule.CopyToLCD(asset);
            }
        }


        /// <summary>
        /// Перенос значений фаз
        /// </summary>
        private void CopyData_phases_To_phase()
        {
            foreach (var asset in importedObjects.Values.OfType<Asset>())
            { 
                asset.phases = asset.phase;
            }
        }

        private void CopyData_normalValue_To_value()
        {
            foreach (var limit in importedObjects.Values.OfType<OperationalLimit>())
            {
                var type = limit.GetType();
                var normalValueProp = type.GetProperty("normalValue");
                var valueProp = type.GetProperty("value");

                if (normalValueProp == null || valueProp == null || normalValueProp.GetValue(limit) == null)
                    continue;

                float normalValueFloat = float.Parse(normalValueProp.GetValue(limit).ToString());
                float valueFloat = float.Parse(valueProp.GetValue(limit).ToString());
                if (normalValueFloat == float.MaxValue ||
                    normalValueFloat == float.MinValue ||
                    normalValueFloat == 0 ||
                    Math.Abs(normalValueFloat - valueFloat) < 0.000001f)
                    continue;

                valueProp.SetValue(limit, Math.Round(normalValueFloat, 6));
            }
        }

        /// <summary>
        /// Копирование breakingCapacity и ratedCurrent
        /// </summary>
        private void CopyData_SwitchInfo_To_Switch()
        {
            foreach (var info in importedObjects.Values.OfType<SwitchInfo>().Where(x => x.ratedCurrent != null || x.breakingCapacity != null))
            {
                foreach (var asset in info.Assets)
                {
                    foreach (var psr in asset.PowerSystemResources)
                    {
                        if (info.breakingCapacity != null && psr is Breaker b)
                            b.breakingCapacity = info.breakingCapacity;

                        if (info.ratedCurrent != null && psr is Switch s)
                            s.ratedCurrent = info.ratedCurrent;
                    }
                }
            }
        }


        /// <summary>
        /// Создает форму с информацией об игнорируемых объектах
        /// </summary>
        private void CreateProtocolForm()
        {
            var ignoredObjects = importedObjects.Values.OfType<TapChangerDependentLimitTable>().Where(x => x.OperationalLimit.Count() == 0);
            if (ignoredObjects.Count() > 0)
            {
                string text = "======== ДЛЯ ЗАВЕРШЕНИЯ ИМПОРТА ЗАКРОЙТЕ ДАННОЕ ОКНО ========" + Environment.NewLine + Environment.NewLine;
                text += "Причина игнорирования: " + "Объект привязывается к несуществующему CurrentLimit или у объекта нет связи с CurrentLimit:" + Environment.NewLine;

                foreach (var elem in ignoredObjects)
                {
                    text += "TapChangerDependentLimitTable" + ": " + elem.Uid.ToString() + Environment.NewLine;

                    var points = elem.TapChangerDependentLimitPoints;
                    ModelImage.RemoveObjects(points);
                    ModelImage.RemoveObject(elem);
                }
#if !WITHOUT_FORM
                Forms.CreateFormIgnoredObj(text);
#endif
            }
        }


        /// <summary>
        /// -
        /// </summary>
        private void SetXForReactors()
        {
            foreach (var v in bPerSections)
            {
                var obj = ModelImage.GetObject<LinearShuntCompensator>(v.Key)?.Assets.FirstOrDefault()?.AssetInfo as ShuntReactorInfo;
                if (obj != null)
                {
                    obj.SetAttribute("x", Math.Round(1 / v.Value, 2));
                }
            }
            bPerSections.Clear();
        }
    }
}
