using Monitel.DataContext.Tools.ModelExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Mal.Context.CIM16;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    public static class SOFullImportRule
    {
        public static void GOSTImportPhase(Asset[] assets)
        {
            
        }
        
        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = Newtonsoft.Json.Linq.JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public static void CopyToLCD(Asset asset)
        {
            if (IsValidJson(asset.inUseDate))
            {
                var jInUseDate = Newtonsoft.Json.Linq.JObject.Parse(asset.inUseDate);
                if (jInUseDate.TryGetValue("inUseDate", out var inUseDate))
                {
                    if (!IsValidJson(asset.lifecycleDate))
                    {
                        var jLifecycleDate = new Newtonsoft.Json.Linq.JObject();
                        jLifecycleDate.Add("initialInServiceDate", inUseDate);
                        var newDate = jLifecycleDate.ToString();
                        newDate = newDate.Replace("\n", string.Empty);
                        newDate = newDate.Replace(" ", string.Empty);
                        newDate = newDate.Replace("\r", string.Empty);
                        asset.lifecycleDate = newDate;
                    }
                    else
                    {
                        var jLifecycleDate = Newtonsoft.Json.Linq.JObject.Parse(asset.lifecycleDate);
                        if (!jLifecycleDate.TryGetValue("initialInServiceDate", out var initialInServiceDate))
                        {
                            jLifecycleDate.Add("initialInServiceDate", inUseDate);
                            var newDate = jLifecycleDate.ToString();
                            newDate = newDate.Replace("\n", string.Empty);
                            newDate = newDate.Replace(" ", string.Empty);
                            newDate = newDate.Replace("\r", string.Empty);
                            asset.lifecycleDate = newDate;
                        }
                        else if (!initialInServiceDate.Any())
                        {
                            jLifecycleDate.Remove("initialInServiceDate");
                            jLifecycleDate.Add("initialInServiceDate", inUseDate);
                            var newDate = jLifecycleDate.ToString();
                            newDate = newDate.Replace("\n", string.Empty);
                            newDate = newDate.Replace(" ", string.Empty);
                            newDate = newDate.Replace("\r", string.Empty);
                            asset.lifecycleDate = newDate;
                        }
                    }
                }
                //asset.inUseDate = string.Empty;
            }
        }

        public static void CopyToLCD(string inUseDateText, string lifecycleDateText, DifferenceObject asset, ClassAttribute lifecycleDate_prop)
        {
            if (IsValidJson(inUseDateText))
            {
                var jInUseDate = Newtonsoft.Json.Linq.JObject.Parse(inUseDateText);
                if (jInUseDate.TryGetValue("inUseDate", out var inUseDate))
                {
                    if (!IsValidJson(lifecycleDateText))
                    {
                        var jLifecycleDate = new Newtonsoft.Json.Linq.JObject();
                        jLifecycleDate.Add("initialInServiceDate", inUseDate);
                        var newDate = jLifecycleDate.ToString();
                        newDate = newDate.Replace("\n", string.Empty);
                        newDate = newDate.Replace("\r", string.Empty);
                        newDate = newDate.Replace(" ", string.Empty);
                        asset.AddString(lifecycleDate_prop, newDate);
                    }
                    else
                    {
                        var jLifecycleDate = Newtonsoft.Json.Linq.JObject.Parse(lifecycleDateText);
                        if (!jLifecycleDate.TryGetValue("initialInServiceDate", out var initialInServiceDate))
                        {
                            jLifecycleDate.Add("initialInServiceDate", inUseDate);
                            var newDate = jLifecycleDate.ToString();
                            newDate = newDate.Replace("\n", string.Empty);
                            newDate = newDate.Replace("\r", string.Empty);
                            newDate = newDate.Replace(" ", string.Empty);
                            asset.AddString(lifecycleDate_prop, newDate);
                        }
                        else if (!initialInServiceDate.Any())
                        {
                            jLifecycleDate.Remove("initialInServiceDate");
                            jLifecycleDate.Add("initialInServiceDate", inUseDate);
                            var newDate = jLifecycleDate.ToString();
                            newDate = newDate.Replace("\n", string.Empty);
                            newDate = newDate.Replace("\r", string.Empty);
                            newDate = newDate.Replace(" ", string.Empty);
                            //newDate = newDate.Replace("{", "{{");
                            //newDate = newDate.Replace("}", "}}");
                            //newDate = newDate.Replace("Т00:00:00", "Т00:00:00Z");
                            asset.AddString(lifecycleDate_prop, newDate);
                        }
                    }
                }
                asset.AddString(lifecycleDate_prop, "");
            }
        }

        public static string ExtractInUseDate(string json)
        {
            if (!IsValidJson(json))
                return "";

            var jsonInUseDate = Newtonsoft.Json.Linq.JObject.Parse(json);
            if (jsonInUseDate.TryGetValue("inUseDate", out var inUseDate))
                return inUseDate.ToString();

            return "";
        }

        public static string ExtractInitialInServiceDate(string json)
        {
            if (!IsValidJson(json))
                return "";

            var jsonInitialInServiceDate = Newtonsoft.Json.Linq.JObject.Parse(json);
            if (jsonInitialInServiceDate.TryGetValue("initialInServiceDate", out var initialInServiceDate))
                return initialInServiceDate.ToString();

            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ModelImage"></param>
        /// <param name="identifiedObjects"></param>
        public static void DoWork(IModelImage ModelImage, IEnumerable<IdentifiedObject> identifiedObjects)
        {
            // Создатель скрипта: Жиленков А.А. (ИА)
            // Ответственный за внесение изменений: Сидоров А.В. (ИА)
            // Редакторы: Семенов Е.А. (ОДУ СЗ), Алехин Р.А. (ОДУ СВ), Сидоров А.В. (ИА)
            //var identifiedObjects = ModelImage.GetObjects<IdentifiedObject>();
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            var root = ModelImage.GetObject<BaseObjectRoot>(new Guid("00000001-0000-0000-C000-0000006D746C")); // тип Monitel.Mal.Context.CIM16.BaseObjectRoot
            var сверху = ModelImage.GetObject<OperationalLimitType>(Guid.Parse("10000CA7-0000-0000-C000-0000006D746C"));
            var СправочникиNode = ModelImage.GetObject<FolderNode>(Guid.Parse("BB42D0B8-2092-4DBB-B0D5-F3470E2BEB88")); //Папка справочников
            var ManufacturerNode = ModelImage.GetObject<FolderNode>(Guid.Parse("9fe21e3d-b80d-470f-acfb-93447357da3d")); //Папка "Роли организации", где находятся все роли всех организаий
            var avarOlsType = ModelImage.GetObject<OperationalLimitSetType>(new Guid("10000C95-0000-0000-C000-0000006D746C")); //аварийный OperationalLimitSetType
            var predOlsType = ModelImage.GetObject<OperationalLimitSetType>(new Guid("10000C9B-0000-0000-C000-0000006D746C")); //предупредительный OperationalLimitSetType
            var oltFolder = ModelImage.GetObject<Folder>(new Guid("29A3B32F-24DA-4B33-9447-E1D49E1DD251")); //все типы экспуатационных пределов
            var bor = ModelImage.GetObject(new Guid("00000001-0000-0000-C000-0000006D746C")); //тип Monitel.Mal.IMalObject
            SetOlsTypes();
            OLSFix();
            VLFix();
            if (ManufacturerNode == null)
            {
                ManufacturerNode = ModelImage.CreateObject<FolderNode>(Guid.Parse("9fe21e3d-b80d-470f-acfb-93447357da3d"));
                ManufacturerNode.folderName = "Роли организации";
            }
            var OrganisationNode = ModelImage.GetObject<FolderNode>(Guid.Parse("8E877299-1A5D-42FA-B405-B0772CDFEFDC")); //Папка "Модели имущественных объектов"
            if (OrganisationNode == null)
            {
                OrganisationNode = ModelImage.CreateObject<FolderNode>(Guid.Parse("8E877299-1A5D-42FA-B405-B0772CDFEFDC"));
                OrganisationNode.folderName = "Модели имущественных объектов";
            }
            int counter = 0;
            var errors = new HashSet<string>();

            //ModelImage.SuspendOnDataChanged();
            try
            {
                foreach (var io in identifiedObjects)
                {
                    try //Выстраиваем дерево по ParentObject
                    {
                        if (io is Terminal terminal)
                            ChangeParentOfTerminal(terminal);
                        else if (io is ConnectivityNode cn)
                            ChangeParentOfConnectivityNode(cn);
                        else if (io is PerLengthSequenceImpedance perLImp)
                            ChangeParentOfPerLengthSequenceImpedance(perLImp);
                        else if (io is ACLineSeriesSection section)
                            ChangeParentOfACLineSeriesSection(section);
                        else if (io is RatioTapChanger ratioTapChanger)
                            ChangeParentOfRatioTapChanger(ratioTapChanger);
                        else if (io is OperationalLimitSet limitSet)
                            ChangeParentOfOperationalLimitSet(limitSet);
                        else if (io is OperationalLimit limit)
                            ChangeParentOfOperationalLimit(limit);
                        else if (io is TapChangerControl tapChangerControl)
                            ChangeParentOfTapChangerControl(tapChangerControl);
                        else if (io is RegulatingControl regulatingControl)
                            ChangeParentOfRegulatingControl(regulatingControl);
                        else if (io is TransformerTest test)
                            ChangeParentOfTransformerTest(test);
                        else if (io is PowerTransformerEnd pte)
                            ChangeParentOfTransformerEnd(pte);
                        else if (io is VoltageLevel vl)
                            ChangeParentOfVoltageLevel(vl);
                        else if (io is Bay bay)
                            ChangeParentOfBay(bay);
                        else if (io is TransformerEndInfo endInfo)
                            ChangeParentOfTransformerEndInfo(endInfo);
                        else if (io is TransformerTankInfo tankInfo)
                            ChangeParentOfTransformerTankInfo(tankInfo);
                        else if (io is TransformerTank tank)
                            ChangeParentOfTransformerTank(tank);
                        else if (io is PowerTransformerInfo ptInfo)
                            ChangeParentOfPowerTransformerInfo(ptInfo);
                        /*
                        try { if (io is NoLoadTest nlTest) ChangeParentOfNoLoadTest(nlTest); }
                    catch (Exception exception) { AddErrorMessageToHashSet(exception, io); }

                    try { if (io is ShortCircuitTest scTest) ChangeParentOfShortCircuitTest(scTest); }
                    catch (Exception exception) { AddErrorMessageToHashSet(exception, io); 
                        */
                        else if (io is WirePosition position)
                            ChangeParentOfWirePosition(position);
                        else if (io is WireSpacingInfo spacing)
                            ChangeParentOfWireSpacingInfo(spacing);
                        /*
                        else if (io is WireAssemblyInfo assembly)
                            ChangeParentOfWireAssemblyInfo(assembly);
                        */
                        else if (io is Line line)
                            ChangeParentOfLine(line);
                        else if (io is Plant plant)
                            ChangeParentOfPlant(plant);
                        else if (io is Substation substation)
                            ChangeParentOfSubstation(substation);
                        else if (io is Manufacturer manufacturer)
                            ChangeParentOfManufacturer(manufacturer);
                        else if (io is Asset || io is AssetContainer || io is Facility)
                        {
                            Asset asset = io as Asset;
                            ChangeParentOfAsset(asset);
                        }
                        else if (io is AssetInfo assetInfo)
                            ChangeParentOfAssetInfo(assetInfo);
                        else if (io is CurrentVsTemperatureLimitCurve ctlCurve)
                            ChangeParentOfCurrentVsTemperatureLimitCurve(ctlCurve);
                        else if (io is CurrentVsTapStepLimitCurve ctslCurve)
                            ChangeParentOfCurrentVsTapStepLimitCurve(ctslCurve);
                        else if (io is CurrentTransformerWinding ctwinding)
                            ChangeParentOfCurrentTransformerWinding(ctwinding);
                        else if (io is PotentialTransformerWinding ptwinding)
                            ChangeParentOfPotentialTransformerWinding(ptwinding);
                        else if (io is AuxiliaryEquipment auxiliaryEquipment)
                            ChangeParentOfAuxiliaryEquipment(auxiliaryEquipment);
                        else if (io is PrimeMover primeMover)
                            ChangeParentOfPrimeMover(primeMover);
                        else if (io is ExcitationControl excitationControl)
                            ChangeParentOfExcitationControl(excitationControl);
                        else if (io is MutualCoupling mutualCoupling)
                            ChangeParentOfMutualCoupling(mutualCoupling);
                        else if (io is Organisation organisation)
                            ChangeParentOfOrganisation(organisation);
                        else if (io is ProductAssetModel model)
                            ChangeParentOfProductAssetModel(model);
                        /*
                        else if (io is TemperatureDependentLimitTable table) 
                            ChangeParentOfTemperatureDependentLimitTable(table);
                        */
                        else if (io is RatioTapChangerTable table)
                            ChangeParentOfRatioTapChangerTable(table);
                        else if (io is SynchronousMachineTimeConstantReactance synchronousMachineTimeConstantReactance)
                            ChangeParentOfSynchronousMachineTimeConstantReactance(synchronousMachineTimeConstantReactance);
                        else if (io is ExcitationSystem excitationSystem)
                            ChangeParentOfExcitationSystem(excitationSystem);
                        else if (io is ReactiveCapabilityCurve reactiveCapabilityCurve)
                            ChangeParentOfReactiveCapabilityCurve(reactiveCapabilityCurve);
                        else if (io is PhaseTapChanger phaseTapChanger)
                            ChangeParentOfPhaseTapChanger(phaseTapChanger);
                        else if (io is AssetDeployment assetDeployment)
                            ChangeParentOfAssetDeployment(assetDeployment);
                        else if (io is TransformerMeshImpedance transformerMeshImpedance)
                            ChangeParentOfTransformerMeshImpedance(transformerMeshImpedance);
                        else if (io is PerLengthPhaseImpedance pi)
                            ChangeParentOfPerLengthPhaseImpedance(pi);
                        else if (io is Equipment equipment)
                            ChangeParentOfEquipment(equipment);
                    }
                    catch (Exception exception)
                    {
                        AddErrorMessageToHashSet(exception, io);
                    }
                }
                fixPowerTransformerInfo();
                fixOperationalLimitTypes();
                OLSPostFix();
                fixMETests();
                AssetPostFix();
            }
            catch (Exception exception)
            {
                //MessageBox.Show(exception.StackTrace.ToString());
                //MessageBox.Show(counter.ToString());
                //Clipboard.SetText(identifiedObjects[counter].Uid.ToString());
            }
            finally
            {
                //ModelImage.ResumeOnDataChanged();
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            DateTime GetInUseDate(string ldt)
            {
                var jo = Newtonsoft.Json.Linq.JObject.Parse(ldt);
                if (jo.TryGetValue("inUseDate", out var initDate))
                    return (DateTime)initDate;
                return DateTime.MinValue;
            }

            void fixMETests()
            {
                ModelImage.RemoveObjects(ModelImage.GetObjects<NoLoadTestME>().Where(x => x.ParentObject == null || x.ParentObject == bor));
                ModelImage.RemoveObjects(ModelImage.GetObjects<ShortCircuitTestME>().Where(x => x.ParentObject == null || x.ParentObject == bor));
            }

            void fixPowerTransformerInfo()
            {
                var ttInfoList = ModelImage.GetObjects<TransformerTankInfo>().Where(x => x.PowerTransformerInfo == null && x.ParentObject != root).Select(ttInfo => (ttInfo, getPTByParent(ttInfo)));
                var ptHash = ttInfoList.Where(x => x.Item2 != null).Select(x => x.Item2).ToHashSet();
                foreach (var t in ptHash)
                {
                    var ptInfo = ModelImage.CreateObject<PowerTransformerInfo>();
                    ptInfo.ParentObject = t;
                    t.AssetDatasheet = ptInfo;
                    ptInfo.name = "Инф. " + t.name;
                    var items = ttInfoList.Where(i => i.Item2 == t).Select(x => x.ttInfo.PowerTransformerInfo = ptInfo);
                    ptInfo.AddRangeToTransformerTankInfos(items.SelectMany(x => x.TransformerTankInfos));
                }
            }

            void fixOperationalLimitTypes()
            {
                var oltList = ModelImage.GetObjects<OperationalLimitType>().Where(x => x.ParentObject == root || x.ParentObject == null);
                var systemOltList = ModelImage.GetObjects<OperationalLimitType>().Where(x => x.ParentObject == oltFolder);
                foreach (var olt in oltList.Where(x => x.OperationalLimit.Any()))
                {
                    olt.ParentObject = root;
                    var systemOlt = systemOltList.FirstOrDefault(x => x.acceptableDuration == olt.acceptableDuration && x.direction == olt.direction);
                    if (systemOlt != null)
                    {
                        olt.OperationalLimit?.ToList().ForEach(x => x.OperationalLimitType = systemOlt);
                        olt.name = "Дубль системного объекта: " + olt.name;
                    }
                    else
                    {
                        olt.name = "Объект не из справочника: " + olt.name;
                    }
                }
            }

            PowerTransformer getPTByParent(IdentifiedObject io)
            {
                var parent = io;
                while (parent != null && parent != root)
                {
                    if (parent is PowerTransformer pt)
                        return pt;
                    parent = parent.ParentObject;
                }
                return null;
            }

            void AddErrorMessageToHashSet(Exception exception, IdentifiedObject io)
            {
                errors.Add($"{exception.Message}\nНе удалось выполнить добавление связи ParentObject для {io.MetaType.Name} ({io.Uid}).");
            }

            void ChangeParentOfPrimeMover(PrimeMover primeMover)
            {
                if (primeMover.SynchronousMachines.Any())
                {
                    primeMover.ParentObject = primeMover.SynchronousMachines.First();
                    counter++;
                }
            }

            void ChangeParentOfTransformerMeshImpedance(TransformerMeshImpedance transformerMeshImpedance)
            {
                if (transformerMeshImpedance.FromTransformerEnd != null)
                {
                    transformerMeshImpedance.ParentObject = (transformerMeshImpedance.FromTransformerEnd as PowerTransformerEnd).PowerTransformer;
                    counter++;
                }
                else if (transformerMeshImpedance.ToTransformerEnd != null)
                {
                    transformerMeshImpedance.ParentObject = (transformerMeshImpedance.ToTransformerEnd as PowerTransformerEnd).PowerTransformer;
                    counter++;
                }
            }

            void ChangeParentOfOrganisation(Organisation organisation)
            {
                if (organisation.Roles.Any(r => r is Manufacturer))
                {
                    if (ModelImage.GetObjects<Folder>().Any(f => f.CreatingNode == OrganisationNode && f.ParentObject == root))
                    {
                        var folder = ModelImage.GetObjects<Folder>().First(f => f.CreatingNode == OrganisationNode && f.ParentObject == root);
                        organisation.ParentObject = folder;
                        counter++;
                    }
                    else
                    {
                        var folder = ModelImage.CreateObject<Folder>();
                        folder.name = "Модели имущественных объектов";
                        folder.CreatingNode = OrganisationNode;
                        folder.ParentObject = root;
                        organisation.ParentObject = folder;
                        counter++;
                    }
                }
            }

            void ChangeParentOfAssetDeployment(AssetDeployment deployment)
            {
                if (deployment.Asset != null)
                {
                    deployment.ParentObject = deployment.Asset;
                    counter++;
                }
            }

            void ChangeParentOfProductAssetModel(ProductAssetModel model)
            {
                if (model.Manufacturer != null && model.Manufacturer.Organisation != null)
                {
                    if (ModelImage.GetObjects<Folder>().Any(f => f.CreatingNode == OrganisationNode && f.ParentObject == model.Manufacturer.Organisation))
                    {
                        var folder = ModelImage.GetObjects<Folder>().First(f => f.CreatingNode == OrganisationNode && f.ParentObject == model.Manufacturer.Organisation);
                        model.ParentObject = folder;
                        counter++;
                    }
                    else
                    {
                        var folder = ModelImage.CreateObject<Folder>();
                        folder.name = "РЗА";
                        folder.CreatingNode = OrganisationNode;
                        folder.ParentObject = model.Manufacturer.Organisation;
                        model.ParentObject = folder;
                        counter++;
                    }
                }
            }

            void ChangeParentOfMutualCoupling(MutualCoupling mutualCoupling)
            {
                if (mutualCoupling.First_Terminal != null)
                {
                    var obj = GetObjectByTerminal(mutualCoupling.First_Terminal);
                    if (obj != null) mutualCoupling.ParentObject = obj;
                    counter++;
                }
                else if (mutualCoupling.Second_Terminal != null)
                {
                    var obj = GetObjectByTerminal(mutualCoupling.Second_Terminal);
                    if (obj != null) mutualCoupling.ParentObject = obj;
                    counter++;
                }
            }

            IdentifiedObject GetObjectByTerminal(Terminal terminal)
            {
                if (terminal.TransformerEnd != null) { return terminal.TransformerEnd; }
                else if (terminal.ConductingEquipment != null) { return terminal.ConductingEquipment; }
                //else if (terminal.ACLineSeriesSection.Any()) { return terminal.ACLineSeriesSection.First(); }
                return null;
            }

            void ChangeParentOfTemperatureDependentLimitTable(TemperatureDependentLimitTable table)
            {
                if (table.OperationalLimit.Any())
                {
                    table.ParentObject = table.OperationalLimit.First();
                    counter++;
                }
            }

            void ChangeParentOfLine(Line line)
            {
                if (line.Region != null)
                {
                    line.ParentObject = line.Region;
                    counter++;
                }
            }

            void ChangeParentOfPlant(Plant plant)
            {
                if (plant.Region != null)
                {
                    plant.ParentObject = plant.Region;
                    counter++;
                }
            }

            void ChangeParentOfSubstation(Substation substation)
            {
                if (substation.Plant != null)
                {
                    substation.ParentObject = substation.Plant;
                    counter++;
                }
                else if (substation.Region != null)
                {
                    substation.ParentObject = substation.Region;
                    counter++;
                }
            }

            void ChangeParentOfCurrentTransformerWinding(CurrentTransformerWinding winding)
            {
                if (winding.Transformer != null)
                {
                    winding.ParentObject = winding.Transformer;
                    counter++;
                }
            }

            void ChangeParentOfPotentialTransformerWinding(PotentialTransformerWinding winding)
            {
                if (winding.Transformer != null)
                {
                    winding.ParentObject = winding.Transformer;
                    counter++;
                }
            }

            void ChangeParentOfTerminal(Terminal terminal)
            {
                if (terminal.TransformerEnd != null)
                {
                    terminal.ParentObject = terminal.TransformerEnd;
                    counter++;
                }
                else if (terminal.ConductingEquipment != null)
                {
                    terminal.ParentObject = terminal.ConductingEquipment;
                    counter++;
                }
                else if (terminal.ACLineSeriesSection.Any())
                {
                    terminal.ParentObject = terminal.ACLineSeriesSection.First();
                    counter++;
                }
            }

            void ChangeParentOfConnectivityNode(ConnectivityNode cn)
            {
                if (cn.ConnectivityNodeContainer != null)
                {
                    cn.ParentObject = cn.ConnectivityNodeContainer;
                    counter++;
                }
            }

            void ChangeParentOfVoltageLevel(VoltageLevel vl)
            {
                if (vl.Substation != null)
                {
                    vl.ParentObject = vl.Substation;
                    counter++;
                }
            }

            void ChangeParentOfBay(Bay bay)
            {
                if (bay.VoltageLevel != null)
                {
                    bay.ParentObject = bay.VoltageLevel;
                    counter++;
                }
            }

            void ChangeParentOfAsset(Asset asset)
            {
                if (true)
                {
                    var potentialParent = asset.PowerSystemResources.FirstOrDefault(x => x is ACLineSeriesSection || x is ACLineSegment || x is TransformerTank || x is PowerTransformer);
                    if (potentialParent != null)
                        asset.ParentObject = potentialParent;
                    else
                        asset.ParentObject = asset.PowerSystemResources.First();
                    counter++;
                }
                else if (asset.AssetContainer != null)
                {
                    asset.ParentObject = asset.AssetContainer;
                    counter++;
                }
            }

            void ChangeParentOfAssetInfo(AssetInfo assetInfo)
            {
                if (assetInfo.Assets.Any())
                {
                    assetInfo.ParentObject = assetInfo.Assets.First();
                    counter++;
                }
                else if (assetInfo.PowerSystemResources.Any())
                {
                    assetInfo.ParentObject = assetInfo.PowerSystemResources.First();
                    counter++;
                }
            }

            void ChangeParentOfTransformerEnd(PowerTransformerEnd pte)
            {
                if (pte.PowerTransformer != null)
                {
                    pte.ParentObject = pte.PowerTransformer;
                    counter++;
                }
            }

            void ChangeParentOfPerLengthSequenceImpedance(PerLengthSequenceImpedance perLImp)
            {
                if (perLImp.ACLineSeriesSection.Any())
                {
                    perLImp.ParentObject = perLImp.ACLineSeriesSection.First();
                    counter++;
                }
                else if (perLImp.ACLineSegments.Any())
                {
                    perLImp.ParentObject = perLImp.ACLineSegments.First();
                    counter++;
                }
            }

            void ChangeParentOfACLineSeriesSection(ACLineSeriesSection section)
            {
                if (section.ACLineSegment != null)
                {
                    section.ParentObject = section.ACLineSegment;
                    counter++;
                }
                else if (section.EquipmentContainer != null)
                {
                    section.ParentObject = section.EquipmentContainer;
                    counter++;
                }
            }

            void ChangeParentOfAuxiliaryEquipment(AuxiliaryEquipment auxiliaryEquipment)
            {
                if (auxiliaryEquipment.Terminal != null)
                {
                    if (auxiliaryEquipment.Terminal.TransformerEnd != null)
                    {
                        auxiliaryEquipment.ParentObject = auxiliaryEquipment.Terminal.TransformerEnd;
                        counter++;
                    }
                    else if (auxiliaryEquipment.Terminal.ConductingEquipment != null)
                    {
                        auxiliaryEquipment.ParentObject = auxiliaryEquipment.Terminal.ConductingEquipment;
                        counter++;
                    }
                }
                else if (auxiliaryEquipment.EquipmentContainer != null)
                {
                    auxiliaryEquipment.ParentObject = auxiliaryEquipment.EquipmentContainer;
                    counter++;
                }
            }

            void ChangeParentOfEquipment(Equipment equipment)
            {
                if (!(equipment is AuxiliaryEquipment) && !(equipment is ACLineSeriesSection))
                    if (equipment.EquipmentContainer != null)
                    {
                        equipment.ParentObject = equipment.EquipmentContainer;
                        counter++;
                    }
            }

            void ChangeParentOfRatioTapChanger(RatioTapChanger ratioTapChanger)
            {
                if (ratioTapChanger.TransformerEnd != null)
                {
                    ratioTapChanger.ParentObject = ratioTapChanger.TransformerEnd;
                    counter++;
                }
            }

            void ChangeParentOfOperationalLimitSet(OperationalLimitSet limitSet)
            {
                if (isPredType(limitSet))
                    limitSet.OperationalLimitSetType = predOlsType;
                else
                    limitSet.OperationalLimitSetType = avarOlsType;
                if (limitSet.Equipment != null)
                {
                    if (limitSet.Equipment is PowerTransformer && limitSet.Terminal != null && limitSet.Terminal is Terminal terminal)
                    {
                        var obj = GetObjectByTerminal(terminal);
                        if (obj != null) limitSet.ParentObject = obj;
                        counter++;
                    }
                    else if (!(limitSet.Equipment is PowerTransformer) && limitSet.Terminal == null)
                    {
                        if (limitSet.Equipment is ConductingEquipment) limitSet.Terminal = (limitSet.Equipment as ConductingEquipment)?.Terminals?.FirstOrDefault();
                        if (limitSet.Equipment is ACLineSeriesSection) limitSet.Terminal = (limitSet.Equipment as ACLineSeriesSection)?.Terminal;
                        if (limitSet.Equipment is AuxiliaryEquipment) limitSet.Terminal = (limitSet.Equipment as AuxiliaryEquipment)?.Terminal;

                        limitSet.ParentObject = limitSet.Equipment;
                        counter++;
                    }
                    else
                    {
                        limitSet.ParentObject = limitSet.Equipment;
                        counter++;
                    }
                }
                else if (limitSet.Terminal != null && limitSet.Terminal is Terminal terminal)
                {
                    var obj = GetObjectByTerminal(terminal);
                    if (obj != null)
                    {
                        limitSet.ParentObject = obj;
                        if (obj is PowerTransformerEnd) limitSet.Equipment = (obj as PowerTransformerEnd).PowerTransformer;
                        else limitSet.Equipment = obj as ConductingEquipment;
                    }
                    counter++;
                }
            }

            /// <summary>
            /// Определяет тип набора ограничений
            /// </summary>
            /// <param name="ols">Все OperationalLimitSet, у которых ParentObject либо BaseObjectRoot, либо null</param>
            /// <returns>True, если Предупредительный набор ограничений, и False, если Аварийный</returns>
            bool isPredType(OperationalLimitSet ols)
            {
                var olv = ols.OperationalLimitValue;
                var vls = olv.OfType<VoltageLimit>(); //все VoltageLimit внутри ols
                foreach (var vl in vls)
                {
                    if (vl.OperationalLimitType?.acceptableDuration != null && vl.OperationalLimitType.acceptableDuration > 0) //у ограничения продолжительность > 0
                        return true;
                }
                var cls = olv.OfType<CurrentLimit>(); //все CurrentLimit внутри ols
                foreach (var cl in cls)
                {
                    if (cl.OperationalLimitType?.acceptableDuration != null && cl.OperationalLimitType.acceptableDuration > 0) //у ограничения продолжительность > 0
                        return true;
                    if (cl.CurrentVsTemperatureLimitCurves.Any(x => x.acceptableDuration > 0)) //у любой кривой I(T) продолжительность ограничения > 0
                        return true;
                }
                if (!string.IsNullOrEmpty(ols.name) && ols.name.ToLower().StartsWith("предупред")) //имя OperationalLimitSet начинается с "предупред"
                    return true;
                return false;
            }

            void ChangeParentOfOperationalLimit(OperationalLimit limit)
            {
                if (limit.OperationalLimitSet != null)
                {
                    limit.ParentObject = limit.OperationalLimitSet;
                    counter++;
                }
            }

            /*void CreateEmergencyOperationalLimitSet(OperationalLimit limit)
            {
                if (limit.OperationalLimitType != сверху ||
                    limit.OperationalLimitSet == null) return;

                var values = limit.OperationalLimitSet.OperationalLimitValue.Where(val => val != limit);
            }*/
            
            void ChangeParentOfTapChangerControl(TapChangerControl tapChangerControl)
            {
                if (tapChangerControl.TapChanger != null)
                {
                    tapChangerControl.ParentObject = tapChangerControl.TapChanger;
                    counter++;
                }
            }

            void ChangeParentOfRegulatingControl(RegulatingControl regulatingControl)
            {
                if (regulatingControl.RegulatingCondEq.Any())
                {
                    regulatingControl.ParentObject = regulatingControl.RegulatingCondEq.FirstOrDefault();
                    counter++;
                }
            }

            void ChangeParentOfTransformerTest(TransformerTest test)
            {
                if (test is NoLoadTest noLoadTest && noLoadTest.EnergisedEnd != null)
                {
                    noLoadTest.ParentObject = noLoadTest.EnergisedEnd;
                    counter++;
                }
                else if (test is ShortCircuitTest shortCircuitTest && shortCircuitTest.EnergisedEnd != null)
                {
                    shortCircuitTest.ParentObject = shortCircuitTest.EnergisedEnd;
                    counter++;
                }
            }

            void ChangeParentOfTransformerEndInfo(TransformerEndInfo endInfo)
            {
                if (endInfo.TransformerTankInfo != null)
                {
                    endInfo.ParentObject = endInfo.TransformerTankInfo;
                    counter++;
                }
                else if (endInfo.PowerTransformerEnd.Any())
                {
                    endInfo.ParentObject = endInfo.PowerTransformerEnd.First();
                    counter++;
                }
                else if (endInfo.StabilizingWindings.Any())
                {
                    endInfo.ParentObject = endInfo.StabilizingWindings.First();
                    counter++;
                }
            }

            void ChangeParentOfTransformerTankInfo(TransformerTankInfo tankInfo)
            {
                if (tankInfo.Assets.Any())
                {
                    tankInfo.ParentObject = tankInfo.Assets.First();
                    counter++;
                }
                else if (tankInfo.PowerSystemResources.Any())
                {
                    tankInfo.ParentObject = tankInfo.PowerSystemResources.First();
                    counter++;
                }
                else if (tankInfo.PowerTransformerInfo != null)
                {
                    tankInfo.ParentObject = tankInfo.PowerTransformerInfo;
                    counter++;
                }
            }

            void ChangeParentOfTransformerTank(TransformerTank tank)
            {
                if (tank.PowerTransformer != null)
                {
                    tank.ParentObject = tank.PowerTransformer;
                    counter++;
                }
            }

            void ChangeParentOfPowerTransformerInfo(PowerTransformerInfo ptInfo)
            {
                if (ptInfo.Assets.Any())
                {
                    ptInfo.ParentObject = ptInfo.Assets.First();
                    counter++;
                }
                else if (ptInfo.PowerSystemResources.Any())
                {
                    ptInfo.ParentObject = ptInfo.PowerSystemResources.First();
                    counter++;
                }
            }

            void ChangeParentOfNoLoadTest(NoLoadTest nlTest)
            {
                if (nlTest.EnergisedEnd != null)
                {
                    nlTest.ParentObject = nlTest.EnergisedEnd.PowerTransformerEnd.First().PowerTransformer;
                    counter++;
                }
            }

            void ChangeParentOfShortCircuitTest(ShortCircuitTest scTest)
            {
                if (scTest.EnergisedEnd != null)
                {
                    scTest.ParentObject = scTest.EnergisedEnd.PowerTransformerEnd.First().PowerTransformer;
                    counter++;
                }
                else if (scTest.GroundedEnds.Any())
                {
                    scTest.ParentObject = scTest.GroundedEnds.First().PowerTransformerEnd.First().PowerTransformer;
                    counter++;
                }
            }

            void ChangeParentOfWirePosition(WirePosition position)
            {
                if (position.WireSpacingInfo != null)
                {
                    position.ParentObject = position.WireSpacingInfo;
                    counter++;
                }
            }

            void ChangeParentOfWireSpacingInfo(WireSpacingInfo spacing)
            {
                var wai = spacing.WirePositions.FirstOrDefault()?.WirePhaseInfo?.FirstOrDefault()?.WireAssemblyInfo;
                if (wai != null)
                {
                    spacing.ParentObject = wai.Assets.First();
                    counter++;
                }
            }

            void ChangeParentOfWireAssemblyInfo(WireAssemblyInfo assembly)
            {
                if (assembly.WirePhaseInfo.Any())
                {
                    assembly.ParentObject = assembly.WirePhaseInfo[0].WireInfo;
                    counter++;
                }
            }

            void ChangeParentOfManufacturer(Manufacturer manufacturer)
            {
                var org = manufacturer.Organisation;
                if (org != null && org.ChildObjects.Any())
                {
                    var manFolder = org.ChildObjects.First(child => child is Folder && (child as Folder).CreatingNode == ManufacturerNode);
                    if (manFolder != null)
                    {
                        manufacturer.ParentObject = manFolder;
                        counter++;
                    }
                    else
                    {
                        var newFolder = ModelImage.CreateObject<Folder>();
                        newFolder.CreatingNode = ManufacturerNode;
                        newFolder.ParentObject = org;
                        newFolder.name = "Роли";
                        manufacturer.ParentObject = newFolder;
                        counter++;
                    }
                }
                else if (org != null && !org.ChildObjects.Any())
                {
                    var newFolder = ModelImage.CreateObject<Folder>();
                    newFolder.CreatingNode = ManufacturerNode;
                    newFolder.ParentObject = org;
                    newFolder.name = "Роли";
                    manufacturer.ParentObject = newFolder;
                    counter++;
                }
            }

            void ChangeParentOfCurrentVsTemperatureLimitCurve(CurrentVsTemperatureLimitCurve ctlCurve)
            {
                if (ctlCurve.CurrentLimits.Any())
                {
                    ctlCurve.ParentObject = ctlCurve.CurrentLimits.First();
                    counter++;
                }
            }

            void ChangeParentOfCurrentVsTapStepLimitCurve(CurrentVsTapStepLimitCurve ctslCurve)
            {
                if (ctslCurve.CurrentLimits.Any())
                {
                    ctslCurve.ParentObject = ctslCurve.CurrentLimits.First();
                    counter++;
                }
            }

            void ChangeParentOfRatioTapChangerTable(RatioTapChangerTable rtcTable)
            {
                if (rtcTable.RatioTapChanger.Any())
                {
                    rtcTable.ParentObject = rtcTable.RatioTapChanger.First();
                    counter++;
                }
            }

            void ChangeParentOfSynchronousMachineTimeConstantReactance(SynchronousMachineTimeConstantReactance synchronousMachineTimeConstantReactance)
            {
                if (synchronousMachineTimeConstantReactance.SynchronousMachine != null)
                {
                    synchronousMachineTimeConstantReactance.ParentObject = synchronousMachineTimeConstantReactance.SynchronousMachine;
                    counter++;
                }
            }

            void ChangeParentOfExcitationControl(ExcitationControl excitationControl)
            {
                if (excitationControl.ExcitationSystem != null)
                {
                    excitationControl.ParentObject = excitationControl.ExcitationSystem;
                    counter++;
                }
            }

            void ChangeParentOfExcitationSystem(ExcitationSystem excitationSystem)
            {
                if (excitationSystem.RotatingMachine != null)
                {
                    excitationSystem.ParentObject = excitationSystem.RotatingMachine;
                    counter++;
                }
            }

            void ChangeParentOfReactiveCapabilityCurve(ReactiveCapabilityCurve reactiveCapabilityCurve)
            {
                if (reactiveCapabilityCurve.SynchronousMachines.Any())
                {
                    reactiveCapabilityCurve.ParentObject = reactiveCapabilityCurve.SynchronousMachines.First();
                    counter++;
                }
            }

            void ChangeParentOfPhaseTapChanger(PhaseTapChanger phaseTapChanger)
            {
                if (phaseTapChanger.TransformerEnd != null)
                {
                    phaseTapChanger.ParentObject = phaseTapChanger.TransformerEnd;
                    counter++;
                }
            }

            void ChangeParentOfPerLengthPhaseImpedance(PerLengthPhaseImpedance pi)
            {
                if (pi.WireAssemblyInfo != null)
                {
                    pi.ParentObject = pi.WireAssemblyInfo;
                    counter++;
                }
            }

            void OLSPostFix()
            {
                var objList = ModelImage.GetObjects<CurrentLimit>().Where(x => x.ParentObject != x.OperationalLimitSet && x.ParentObject is OperationalLimitSet);
                foreach (var o in objList)
                {
                    o.OperationalLimitSet = o.ParentObject as OperationalLimitSet;
                    foreach (var c in o.CurrentVsTemperatureLimitCurves)
                    {
                        c.RemoveAllCurrentLimits();
                        c.AddToCurrentLimits(o);
                    }
                }
            }

            /// <summary>
            /// Присваивает тип (Предупредительный или Аварийный) для каждого OperationalLimitSetType, у которого ParentObject либо BaseObjectRoot, либо null
            /// </summary>
            void SetOlsTypes()
            {
                //ModelImage.SuspendOnDataChanged();
                var objs = ModelImage.GetObjects<OperationalLimitSet>().Where(x => x.ParentObject == bor || x.ParentObject == null);
                foreach (var obj in objs)
                {
                    if (isPredType(obj))
                        obj.OperationalLimitSetType = predOlsType;
                    else
                        obj.OperationalLimitSetType = avarOlsType;
                }
                //ModelImage.ResumeOnDataChanged();
            }

            /// <summary>
            /// Создает аварийные наборы ограничений для потерянных в модели VoltageLimit
            /// </summary>
            void VLFix()
            {
                //ModelImage.SuspendOnDataChanged();
                var objs = ModelImage.GetObjects<VoltageLimit>().Where(x => x.ParentObject == bor || x.ParentObject == null);
                var upOlType = ModelImage.GetObject<OperationalLimitType>(new Guid("10000CA7-0000-0000-C000-0000006D746C"));
                var groups = objs.Select(x => x.OperationalLimitSet).Where(x => x != null && x.OperationalLimitValue.Any()).ToHashSet();
                foreach (var ols in groups)
                {
                    var avarLimit = ols.OperationalLimitValue.OfType<VoltageLimit>().FirstOrDefault(x => x.OperationalLimitType == upOlType);
                    if (avarLimit != null)
                    {
                        var eq = avarLimit.OperationalLimitSet.Equipment;
                        OperationalLimitSet avarOls = null;
                        if (eq == null || eq is ACLineSegment || eq is PowerTransformer)
                        {
                            var terminal = ols.Terminal;
                            avarOls = terminal?.OperationalLimitSet?.FirstOrDefault(x => x.OperationalLimitSetType == avarOlsType || isPredType(x) == false);
                        }
                        else
                        {
                            avarOls = eq.OperationalLimitSet.FirstOrDefault(x => x.OperationalLimitSetType == avarOlsType || isPredType(x) == false);
                        }
                        if (avarOls == null)
                        {
                            avarOls = ModelImage.CreateObject<OperationalLimitSet>();
                            avarOls.name = "Аварийный набор ограничений";
                            avarOls.Equipment = eq;
                            if (ols.Terminal != null)
                                avarOls.Terminal = ols.Terminal;
                            avarOls.OperationalLimitSetType = avarOlsType;
                        }
                        avarOls.AddToOperationalLimitValue(avarLimit);
                    }
                }
                //ModelImage.ResumeOnDataChanged();
            }


            /// <summary>
            /// Создает аварийные наборы ограничений для потерянных в модели CurrentLimit
            /// </summary>
            void OLSFix()
            {
                //ModelImage.SuspendOnDataChanged();
                var objs = ModelImage.GetObjects<CurrentLimit>().Where(x => x.ParentObject == bor || x.ParentObject == null); //Все CurrentLimit, у которых ParentObject либо BaseObjectRoot, либо null
                var olsHashSet = objs.Select(x => x.OperationalLimitSet).Where(x => x != null && x.OperationalLimitValue.Count() > 0).ToHashSet(); //not null OperationalLimitSet у всех объектов CurrentLimit
                HashSet<IMalObject> listToRemove = new HashSet<IMalObject>();
                int last = 0;
                foreach (var ols in olsHashSet)
                {
                    try
                    {
                        var tapStepList = ols.OperationalLimitValue.Where(x => x is CurrentLimit cl && cl.CurrentVsTapStepLimitCurves.Any()); //Список CurrentLimit из OperationalLimitSet, у которых есть I(T)
                        var groups = ols.OperationalLimitValue.Where(x => x is CurrentLimit cl).GroupBy(x => (x as CurrentLimit).value); //Группировка CurrentLimit по CurrentLimit.value
                        if (groups.Any())
                        {
                            foreach (var g in groups.Where(x => x.Any())) //Для каждой группы, где есть элементы
                            {
                                var tGroups = g.GroupBy(x => x.OperationalLimitSet.Terminal?.Uid); //Группировка CurrentLimit с одним CurrentLimit.value по UID терминала, к которому принадлежит OperationalLimitSet
                                foreach (var gg in tGroups) //Для каждой группы одинаковых CurrentLimit.value соответствующего терминала 
                                {
                                    var avarLimit = gg.FirstOrDefault(x => x.OperationalLimitType?.acceptableDuration == null); //Первый CurrentLimit, у которого не задана продолжительность ограничения
                                    if (avarLimit != null)
                                    {
                                        var eq = avarLimit.OperationalLimitSet.Equipment;
                                        OperationalLimitSet avarOls = null;
                                        if (eq == null || eq is ACLineSegment || eq is PowerTransformer) //CurrentLimit не привязан к оборудованию или оборудование - сегмент линии/ТР
                                        {
                                            var terminal = ols.Terminal;
                                            avarOls = terminal?.OperationalLimitSet?.FirstOrDefault(x => x.OperationalLimitSetType == avarOlsType || isPredType(x) == false); //Аварийный набор ограничений
                                            if (eq is PowerTransformer pt && terminal == null) //CurrentLimit привязан к ТР, но у OperationalLimitSet нет терминала
                                            {
                                                avarOls = pt.OperationalLimitSet.FirstOrDefault(x => x.Terminal == null && x.OperationalLimitSetType == avarOlsType);
                                            }
                                        }
                                        else
                                        {
                                            avarOls = eq.OperationalLimitSet.FirstOrDefault(x => x.OperationalLimitSetType == avarOlsType || isPredType(x) == false);
                                        }
                                        if (avarOls == null)
                                        {
                                            avarOls = ModelImage.CreateObject<OperationalLimitSet>();
                                            avarOls.name = "Аварийный набор ограничений";
                                            avarOls.Equipment = eq;
                                            if (ols.Terminal != null)
                                                avarOls.Terminal = ols.Terminal;
                                            avarOls.OperationalLimitSetType = avarOlsType;
                                        }
                                        avarOls.AddToOperationalLimitValue(avarLimit);
                                        var objsToBeRemoved = getObjectsToBeRemoved(avarOls.OperationalLimitValue.OfType<CurrentLimit>(), out CurrentLimit avarCl);
                                        listToRemove.UnionWith(objsToBeRemoved);
                                        CurrentLimit pcl = null;
                                        if (gg.Any(x => x != avarLimit))
                                        {
                                            var objsToBeRemoved2 = getObjectsToBeRemoved(gg.Where(x => x != avarLimit).OfType<CurrentLimit>(), out CurrentLimit predCl);
                                            listToRemove.UnionWith(objsToBeRemoved2);
                                            pcl = predCl;
                                        }
                                        foreach (var taps in tapStepList.Where(x => x != avarLimit && x != pcl))
                                        {
                                            if (pcl != null && pcl?.value == (taps as CurrentLimit)?.value)
                                            {
                                                pcl.AddRangeToCurrentVsTapStepLimitCurves((taps as CurrentLimit).CurrentVsTapStepLimitCurves);
                                                listToRemove.Add(taps);
                                            }
                                            else if (avarCl != null && avarCl?.value == (taps as CurrentLimit)?.value)
                                            {
                                                avarCl.AddRangeToCurrentVsTapStepLimitCurves((taps as CurrentLimit).CurrentVsTapStepLimitCurves);
                                                listToRemove.Add(taps);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        CurrentLimit pcl = null;
                                        var objsToBeRemoved = getObjectsToBeRemoved(gg.OfType<CurrentLimit>(), out CurrentLimit predCl);
                                        listToRemove.UnionWith(objsToBeRemoved);
                                        pcl = predCl;
                                        foreach (var taps in tapStepList.Where(x => x != avarLimit && x != predCl))
                                        {
                                            if (pcl != null && pcl?.value == (taps as CurrentLimit)?.value)
                                            {
                                                pcl.AddRangeToCurrentVsTapStepLimitCurves((taps as CurrentLimit).CurrentVsTapStepLimitCurves);
                                                listToRemove.Add(taps);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                }
                ModelImage.RemoveObjects(listToRemove);
                //ModelImage.ResumeOnDataChanged();
            }

            void AssetPostFix()
            {
                var assetList = ModelImage.GetObjects<Asset>().Where(x => x is Asset || x is AssetContainer || x is Facility);
                foreach (var asset in assetList)
                {
                    if (asset.inUseDate != null && asset.inUseDate != string.Empty)
                    {
                        var jInUseDate = Newtonsoft.Json.Linq.JObject.Parse(asset.inUseDate);
                        if (jInUseDate.TryGetValue("inUseDate", out var inUseDate))
                        {
                            if (asset.lifecycleDate == null || asset.lifecycleDate == string.Empty)
                            {
                                var jLifecycleDate = new Newtonsoft.Json.Linq.JObject();
                                jLifecycleDate.Add("initialInServiceDate", inUseDate);
                                var newDate = jLifecycleDate.ToString();
                                newDate = newDate.Replace("\n", string.Empty);
                                newDate = newDate.Replace(" ", string.Empty);
                                asset.lifecycleDate = newDate;
                            }
                            else if (asset.lifecycleDate != null)
                            {
                                var jLifecycleDate = Newtonsoft.Json.Linq.JObject.Parse(asset.lifecycleDate);
                                if (!jLifecycleDate.TryGetValue("initialInServiceDate", out var initialInServiceDate))
                                {
                                    jLifecycleDate.Add("initialInServiceDate", inUseDate);
                                    var newDate = jLifecycleDate.ToString();
                                    newDate = newDate.Replace("\n", string.Empty);
                                    newDate = newDate.Replace(" ", string.Empty);
                                    asset.lifecycleDate = newDate;
                                }
                                else
                                {
                                    if (!initialInServiceDate.Any())
                                    {
                                        jLifecycleDate.Remove("initialInServiceDate");
                                        jLifecycleDate.Add("initialInServiceDate", inUseDate);
                                        var newDate = jLifecycleDate.ToString();
                                        newDate = newDate.Replace("\n", string.Empty);
                                        newDate = newDate.Replace(" ", string.Empty);
                                        asset.lifecycleDate = newDate;
                                    }
                                }
                            }
                        }
                        //asset.inUseDate = string.Empty;
                    }
                }
            }

            HashSet<IMalObject> getObjectsToBeRemoved(IEnumerable<CurrentLimit> g, out CurrentLimit predCl)
            {
                bool notFound = false;
                HashSet<IMalObject> listToRemove = new HashSet<IMalObject>();
                var soLimits = g.Where(x => x.ParentObject is OperationalLimitSet);
                CurrentLimit cl_so = g.FirstOrDefault(x => x.ParentObject is OperationalLimitSet);
                var upOlType = ModelImage.GetObject<OperationalLimitType>(new Guid("10000CA7-0000-0000-C000-0000006D746C"));
                if (cl_so == null)
                {
                    cl_so = g.FirstOrDefault();
                    notFound = true;
                }
                if (notFound)
                {
                    if (cl_so.CurrentVsTemperatureLimitCurves.Any())
                    {
                        foreach (var curve in cl_so.CurrentVsTemperatureLimitCurves)
                        {
                            curve.acceptableDuration = cl_so.OperationalLimitType.acceptableDuration;
                            //curve.description = "new";
                        }
                        cl_so.OperationalLimitType = upOlType;
                        cl_so.name = "Imax";
                    }
                }
                predCl = cl_so;
                foreach (CurrentLimit cl in g.Where(x => x != cl_so))
                {
                    if (cl.CurrentVsTemperatureLimitCurves.Any())
                    {
                        foreach (var curve in cl.CurrentVsTemperatureLimitCurves)
                        {
                            curve.acceptableDuration = cl.OperationalLimitType.acceptableDuration;
                            //curve.description = "new";
                            cl_so.AddToCurrentVsTemperatureLimitCurves(curve);
                        }
                        cl_so.OperationalLimitType = upOlType;
                    }
                    if (!soLimits.Contains(cl))
                        listToRemove.Add(cl);
                }
                var curveGroups = cl_so.CurrentVsTemperatureLimitCurves.GroupBy(x => x.acceptableDuration).ToList();
                foreach (var curveGroup in curveGroups.Where(x => x.Count() > 1))
                {
                    if (curveGroup.Count() > 2)
                    {
                        //Clipboard.SetText(cl_so.Uid.ToString());
                        //throw new Exception("Во время работы скрипта обнаружен предел с графиками с одинаковым acceptableDuration>2. Его UID скопирован в буфер обмена");
                    }
                    if (curveGroup.Count() > 1)
                    {
                        var curve_so = curveGroup.FirstOrDefault(x => x.ParentObject is OperationalLimit);
                        if (curve_so != null)
                        {
                            curve_so.description = "SO";
                            var curvesToDelete = curveGroup.Where(x => x.description == "SO");
                            if (curvesToDelete.Any())
                                listToRemove.UnionWith(curvesToDelete);
                        }
                    }
                }
                return listToRemove;
            }
        }
    }
}
