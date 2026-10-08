using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using Monitel.DataContext.Tools.ModelExtensions;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;
using Monitel.PlatformInfrastructure;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions
{
    public interface IConversionStrategyImport
    {
        // Определяет, может ли стратегия обработать этот объект
        bool CanHandle(string className, XElement xmlElement);
        bool CanHandle(DifferenceObject foGost, DifferenceObject roGost);

        // Импортирует элемент со всеми свойствами
        void ImportElement(XElement xmlElement);
        void ImportElement(DifferenceObject foGost, DifferenceObject roGost);
    }

    public abstract class ConversionStrategyImport : IConversionStrategyImport
    {
        // ИМ
        protected IModelImage modelImage;

        // PSRType СЭС или ВЭС
        protected PSRType psrRType;

        // Метасущность класса, из которого необходимо преобразовать
        protected MetaClass metaClassToConvert;

        // Метасущность класса, в который необходимо преобразовать
        protected MetaClass metaClassConverted;

        // Словарь свойств ключ - свойство класса, который нужно экспортировать, значение - свойство класса, в который нужно экспортировать
        protected Dictionary<ClassProperty, ClassProperty> propertyMappings;

        public ConversionStrategyImport(IModelImage modelImage)
        {
            this.modelImage = modelImage;
            psrRType = SetPSRType();
            metaClassToConvert = SetMetaClassToConvert();
            metaClassConverted = SetMetaClassConverted();
            propertyMappings = SetPropertyMapping();
        }

        /// Сеттеры
        protected abstract Dictionary<ClassProperty, ClassProperty> SetPropertyMapping();
        protected abstract PSRType SetPSRType();
        protected abstract MetaClass SetMetaClassToConvert();
        protected abstract MetaClass SetMetaClassConverted();

        /// Реализация интерфейса IConversionStrategyImport
        public virtual bool CanHandle(string className, XElement xmlElement) => className == metaClassToConvert.Name;
        public virtual bool CanHandle(DifferenceObject foGost, DifferenceObject roGost)
        {
            // при Description класс объекта не указан, так что надо определить
            if (foGost?.IsDescription == true || roGost?.IsDescription == true)
            {
                var objInModel = modelImage.GetObject((foGost ?? roGost).ObjectUid);
                if (objInModel == null)
                    return false;

                if (this.psrRType != null)
                    return IsPartOfStationWithPSRType(objInModel, this.psrRType) && objInModel.MetaType.Name == this.metaClassConverted.Name;
                else
                    return (IsPartOfStationWithPSRType(objInModel, PsrTypes.SolarPlant(modelImage)) || IsPartOfStationWithPSRType(objInModel, PsrTypes.WindPlant(modelImage)))
                           && objInModel.MetaType.Name == this.metaClassConverted.Name;
            }
            return (foGost ?? roGost)?.ObjectClass?.Name == metaClassToConvert.Name;
        }

        /// <summary>
        /// Проверка, относится ли объект к конкретному типу станции
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected bool IsPartOfStationWithPSRType(IMalObject obj, PSRType psrRType)
        {
            var io = modelImage.GetObject<IdentifiedObject>(obj.Uid);
            int k = 0;
            while (io?.ParentObject?.Uid != Guid.Parse("00000001-0000-0000-C000-0000006D746C") && io?.ParentObject != null && k < 50)
            {
                if (io?.ParentObject is Plant plant && plant.PSRType == psrRType)
                {
                    return true;
                }
                io = io?.ParentObject;
                k++;
            }
            return false;
        }

        public virtual void ImportElement(XElement xmlElement)
        {
            var guid = new Guid(xmlElement.FirstAttribute.Value.Substring(2));
            var obj = GetObject(guid);

            // Проходим по всем свойствам, которые заданы в xml
            var propsXML = xmlElement.Elements();
            foreach (var propXML in propsXML)
            {
                var nameParts = propXML.Name.LocalName.Split('.');
                var propXmlName = nameParts[nameParts.Length - 1];

                var propModel = propertyMappings.Keys.FirstOrDefault(x => x.Name == propXmlName);
                if (propModel != null)
                {
                    // Получаем значение свойства
                    var valueString = GetPropValue(propXML);
                    ImportValue(propertyMappings[propModel], obj, valueString);
                }
            }
            CreateAdditionalChangesToObj(obj, xmlElement);
        }

        public virtual void ImportElement(DifferenceObject foGost, DifferenceObject roGost)
        {
            // Если только удаление объекта
            if (foGost == null && roGost != null && roGost.IsDescription == false)
            {
                this.modelImage.RemoveObject(this.modelImage.GetObject(roGost.ObjectUid));
                return;
            }

            var guid = (foGost ?? roGost).ObjectUid;
            var obj = GetObject(guid);

            var props_foGost_roGost = (foGost ?? roGost).Properties;
            foreach (var propDiffModel in props_foGost_roGost)
            {
                var propModel = propertyMappings.Keys.FirstOrDefault(x => x.Name == propDiffModel.Name);
                if (propModel != null)
                {
                    string[] valuesString_foGost;
                    try
                    {
                        ImportValue(propertyMappings[propModel], obj, GetPropValue(foGost, propDiffModel));
                    }
                    catch
                    {
                        ImportDefaultValue_DeleteAssoc(propertyMappings[propModel], obj, "");
                    }
                }
            }

            CreateAdditionalChangesToObj(obj, foGost, roGost);
        }

        protected abstract IdentifiedObject GetObject(Guid guid);

        /// <summary>
        /// Получение значения свойства
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected string GetPropValue(XElement propXML)
        {
            // ассоциации и поля с Enum
            if (propXML.FirstAttribute != null)
            {
                var res = propXML.FirstAttribute.Value.Replace("#_", "");
                return res.Split('.').Last();
            }
            // атрибуты
            return propXML.Value;
        }

        /// <summary>
        /// Получение значения свойства
        /// </summary>
        /// <param name="foGost"></param>
        /// <param name="targetProp"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected string GetPropValue(DifferenceObject elem, ClassProperty targetProp)
        {
            switch (targetProp.Kind)
            {
                case PropertyKind.Attribute:
                    string convertedValue = targetProp.StoredType switch //  Преобразование типа 
                    {
                        PrimitiveType.String => elem.GetString(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Int16 => elem.GetInt16(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Int32 => elem.GetInt32(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Int64 => elem.GetInt64(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Float32 => elem.GetFloat32(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Float64 => elem.GetFloat64(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Bit => elem.GetBool(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.DateTime => elem.GetDateTime(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.Guid => elem.GetGuid(targetProp as ClassAttribute).ToString(),
                        PrimitiveType.EnumValue => elem.GetEnum(targetProp as ClassAttribute).ToString(),
                        _ => throw new NotSupportedException($"Не удается определить значение атрибута с типом {targetProp.StoredType}.")
                    };
                    return convertedValue;

                case PropertyKind.AssocToOne:
                    return elem.GetToOneUid(targetProp as ClassAssociation).ToString();

                case PropertyKind.AssocToMany:
                    return String.Join(";", elem.GetToManyUids(targetProp as ClassAssociation));
            }
            return "";
        }

        /// <summary>
        /// Перенос значения свойства
        /// </summary>
        /// <param name="targetProp"></param>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void ImportValue(ClassProperty targetProp, IdentifiedObject obj, string value)
        {
            switch (targetProp.Kind)
            {
                case PropertyKind.Attribute:
                    object convertedValue = targetProp.StoredType switch //  Преобразование типа 
                    {
                        PrimitiveType.String => value,
                        PrimitiveType.Int16 => short.Parse(value),
                        PrimitiveType.Int32 => int.Parse(value),
                        PrimitiveType.Int64 => long.Parse(value),
                        PrimitiveType.Float32 => float.Parse(value.Replace(".", ",")),
                        PrimitiveType.Float64 => double.Parse(value.Replace(".", ",")),
                        PrimitiveType.Bit => bool.Parse(value),     
                        PrimitiveType.DateTime => DateTime.Parse(value),
                        PrimitiveType.Guid => Guid.Parse(value),
                        PrimitiveType.EnumValue => GetMetaEnumValue(targetProp, value),
                        _ => throw new NotSupportedException($"Тип {targetProp.StoredType} не поддерживается.")
                    };
                    obj.SetAttribute(targetProp.Name, convertedValue);
                    break;

                case PropertyKind.AssocToOne:
                    var objToOne = modelImage.GetObject(Guid.Parse(value));
                    if (objToOne != null)
                        obj.SetAssoc1To(targetProp as ClassAssociation, objToOne);
                    break;

                case PropertyKind.AssocToMany:
                    var uidsString = value.Split(';').ToList();
                    foreach (var uidString in uidsString)
                    {
                        var objToMany = modelImage.GetObject(Guid.Parse(uidString));
                        if (objToMany != null)
                            obj.AddToAssocM(targetProp as ClassAssociation, objToMany);
                    }
                    break;
            }
        }

        /// <summary>
        /// Выставляет значение атирибута по-умолчанию, удаляет объект из ассоциации
        /// </summary>
        /// <param name="targetProp"></param>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        protected void ImportDefaultValue_DeleteAssoc(ClassProperty targetProp, IdentifiedObject obj, string value)
        {
            switch (targetProp.Kind)
            {
                case PropertyKind.Attribute:
                    var attr = targetProp as ClassAttribute;
                    object convertedDefaultValue = targetProp.StoredType switch //  Преобразование типа 
                    {
                        PrimitiveType.String => attr.DefaultValue,
                        PrimitiveType.Int16 => short.Parse(attr.DefaultValue),
                        PrimitiveType.Int32 => int.Parse(attr.DefaultValue),
                        PrimitiveType.Int64 => long.Parse(attr.DefaultValue),
                        PrimitiveType.Float32 => float.Parse(attr.DefaultValue.Replace(".", ",")),
                        PrimitiveType.Float64 => double.Parse(attr.DefaultValue.Replace(".", ",")),
                        PrimitiveType.Bit => bool.Parse(attr.DefaultValue),
                        PrimitiveType.DateTime => DateTime.Parse(attr.DefaultValue),
                        PrimitiveType.Guid => Guid.Parse(attr.DefaultValue),
                        PrimitiveType.EnumValue => GetMetaEnumValue(targetProp, attr.DefaultValue),
                        _ => throw new NotSupportedException($"Тип {targetProp.StoredType} не поддерживается.")
                    };
                    obj.SetAttribute(targetProp.Name, convertedDefaultValue);
                    break;

                case PropertyKind.AssocToOne:
                    obj.SetAssoc1To(targetProp as ClassAssociation, null);
                    break;

                case PropertyKind.AssocToMany:
                    var uidsString = value.Split(';').ToList();
                    foreach (var uidString in uidsString)
                    {
                        var objToMany = modelImage.GetObject(Guid.Parse(uidString));
                        if (objToMany != null)
                            obj.RemoveFromAssocM(targetProp as ClassAssociation, objToMany);
                    }
                    break;
            }
        }

        /// <summary>
        /// Возвращает значение enum по его строковому описанию
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private MetaEnumValue GetMetaEnumValue(ClassProperty prop, string value)
        {
            var elemEnumByName = modelImage.MetaData.Enums[prop.Name].Values.FirstOrDefault(x => x.Name == value);
            if (elemEnumByName != null)
                return elemEnumByName;

            int intValue;
            if (int.TryParse(value, out intValue))
            {
                var elemEnumByNumber = modelImage.MetaData.Enums[prop.Name].Values.FirstOrDefault(x => x.Id == intValue);
                if (elemEnumByNumber != null)
                    return elemEnumByNumber;
            }

            return null;
        }

        /// <summary>
        /// Внесение дополнительных изменений в объект при импорте
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement) { }
        protected virtual void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost) { }
        protected void CreateChangesOnAssocToMany(IdentifiedObject obj, ClassProperty prop_model, XElement prop_XML)
        {
            if (prop_XML != null)
            {
                var valueString = GetPropValue(prop_XML);
                ImportValue(prop_model, obj, valueString);
            }
        }
        protected void CreateChangesOnAssocToMany(IdentifiedObject obj, ClassProperty prop_model, DifferenceObject foGost, DifferenceObject roGost, ClassProperty prop_foGost, ClassProperty prop_roGost)
        {
            // если есть только в reverse
            if (prop_roGost != null && prop_foGost == null)
            {
                var valueString = GetPropValue(roGost, prop_roGost);
                ImportDefaultValue_DeleteAssoc(prop_model, obj, valueString);
            }
            // если есть только в forward
            else if (prop_roGost == null && prop_foGost != null)
            {
                var valueString = GetPropValue(foGost, prop_foGost);
                ImportValue(prop_model, obj, valueString);
            }
            // если есть и в forward и в reverse
            else if (prop_roGost != null && prop_foGost != null)
            {
                // сначала удаляем старую ассоциацию (тк множественная)
                var valueString = GetPropValue(roGost, prop_roGost);
                ImportDefaultValue_DeleteAssoc(prop_model, obj, valueString);

                // затем добавляем новую ассоциацию
                valueString = GetPropValue(foGost, prop_foGost);
                ImportValue(prop_model, obj, valueString);
            }
        }
    }


    /// <summary>
    /// PhotoVoltaicUnit -> ThermalGeneratingUnit
    /// </summary>
    public class PhotoVoltaicUnit_To_ThermalGeneratingUnit_Import : ConversionStrategyImport
    {
        public PhotoVoltaicUnit_To_ThermalGeneratingUnit_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.SolarPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(PhotoVoltaicUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)]; 
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "trackerType"), metaClassConverted.AllProperties.First(x => x.Name == "trackerType"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "governorSCD"), metaClassConverted.AllProperties.First(x => x.Name == "governorSCD"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxP"), metaClassConverted.AllProperties.First(x => x.Name == "maxOperatingP")); 
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minP"), metaClassConverted.AllProperties.First(x => x.Name == "minOperatingP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PowerElectronicsConnection"), metaClassConverted.AllProperties.First(x => x.Name == "RotatingMachine"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Equipment)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<ThermalGeneratingUnit>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<ThermalGeneratingUnit>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }
    }


    /// <summary>
    /// PowerElectronicsConnection -> SynchronousMachine
    /// </summary>
    public class PowerElectronicsConnection_To_SynchronousMachine_Import : ConversionStrategyImport
    {
        public PowerElectronicsConnection_To_SynchronousMachine_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return null; // тк может быть и в СЭС и в ВЭС
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsConnection)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(SynchronousMachine)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxQ"), metaClassConverted.AllProperties.First(x => x.Name == "maxQ"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minQ"), metaClassConverted.AllProperties.First(x => x.Name == "minQ"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedPowerFactor"), metaClassConverted.AllProperties.First(x => x.Name == "ratedPowerFactor"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedS"), metaClassConverted.AllProperties.First(x => x.Name == "ratedS"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedU"), metaClassConverted.AllProperties.First(x => x.Name == "ratedU"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PowerElectronicsUnit"), metaClassConverted.AllProperties.First(x => x.Name == "GeneratingUnit")); 
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PowerElectronicsReactiveCapabilityCurve"), metaClassConverted.AllProperties.First(x => x.Name == "InitialReactiveCapabilityCurve")); 
            foreach (var prop in modelImage.MetaData.Classes[nameof(RegulatingCondEq)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<SynchronousMachine>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<SynchronousMachine>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement)
        {
            var syncronousMachine = obj as SynchronousMachine;
            syncronousMachine.type = SynchronousMachineKind.generator;

            var reactiveCapabilityCurves_model = metaClassConverted.AllProperties.First(x => x.Name == "ReactiveCapabilityCurves");
            var powerElectronicsReactiveCapabilityCurve_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "PowerElectronicsReactiveCapabilityCurve");

            CreateChangesOnAssocToMany(syncronousMachine, reactiveCapabilityCurves_model, powerElectronicsReactiveCapabilityCurve_propXML);
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost)
        {
            var syncronousMachine = obj as SynchronousMachine;
            syncronousMachine.type = SynchronousMachineKind.generator;

            var reactiveCapabilityCurves_model = metaClassConverted.AllProperties.First(x => x.Name == "ReactiveCapabilityCurves");
            var powerElectronicsReactiveCapabilityCurve_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "PowerElectronicsReactiveCapabilityCurve");
            var powerElectronicsReactiveCapabilityCurve_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "PowerElectronicsReactiveCapabilityCurve");

            CreateChangesOnAssocToMany(syncronousMachine, reactiveCapabilityCurves_model, foGost, roGost, powerElectronicsReactiveCapabilityCurve_foGost, powerElectronicsReactiveCapabilityCurve_roGost);
        }
    }


    /// <summary>
    /// WindGeneratingUnit -> ThermalGeneratingUnit
    /// </summary>
    public class WindGeneratingUnit_To_ThermalGeneratingUnit_Import : ConversionStrategyImport
    {
        public WindGeneratingUnit_To_ThermalGeneratingUnit_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(WindGeneratingUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            foreach (var prop in modelImage.MetaData.Classes[nameof(GeneratingUnit)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<ThermalGeneratingUnit>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<ThermalGeneratingUnit>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }
    }


    /// <summary>
    /// PowerElectronicsWindUnit -> ThermalGeneratingUnit
    /// </summary>
    public class PowerElectronicsWindUnit_To_ThermalGeneratingUnit_Import : ConversionStrategyImport
    {
        public PowerElectronicsWindUnit_To_ThermalGeneratingUnit_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsWindUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "governorSCD"), metaClassConverted.AllProperties.First(x => x.Name == "governorSCD"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxP"), metaClassConverted.AllProperties.First(x => x.Name == "maxOperatingP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minP"), metaClassConverted.AllProperties.First(x => x.Name == "minOperatingP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PowerElectronicsConnection"), metaClassConverted.AllProperties.First(x => x.Name == "RotatingMachine"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Equipment)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<ThermalGeneratingUnit>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<ThermalGeneratingUnit>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }
    }


    /// <summary>
    /// AsynchronousMachine -> SynchronousMachine
    /// </summary>
    public class AsynchronousMachine_To_SynchronousMachine_Import : ConversionStrategyImport
    {
        public AsynchronousMachine_To_SynchronousMachine_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(AsynchronousMachine)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(SynchronousMachine)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ReactiveCapabilityCurve"), metaClassConverted.AllProperties.First(x => x.Name == "InitialReactiveCapabilityCurve"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PrimeMover"), metaClassConverted.AllProperties.First(x => x.Name == "PrimeMovers"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(RotatingMachine)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<SynchronousMachine>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<SynchronousMachine>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement)
        {
            var syncronousMachine = obj as SynchronousMachine;
            syncronousMachine.type = SynchronousMachineKind.generator;

            var reactiveCapabilityCurves_model = metaClassConverted.AllProperties.First(x => x.Name == "ReactiveCapabilityCurves");
            var reactiveCapabilityCurve_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "ReactiveCapabilityCurve");

            CreateChangesOnAssocToMany(syncronousMachine, reactiveCapabilityCurves_model, reactiveCapabilityCurve_propXML);

            var primeMovers_model = metaClassConverted.AllProperties.First(x => x.Name == "PrimeMovers");
            var primeMover_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "PrimeMover");

            CreateChangesOnAssocToMany(syncronousMachine, primeMovers_model, primeMover_propXML);
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost)
        {
            var syncronousMachine = obj as SynchronousMachine;
            syncronousMachine.type = SynchronousMachineKind.generator;

            var reactiveCapabilityCurves_model = metaClassConverted.AllProperties.First(x => x.Name == "ReactiveCapabilityCurves");
            var reactiveCapabilityCurve_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "ReactiveCapabilityCurve");
            var reactiveCapabilityCurve_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "ReactiveCapabilityCurve");

            CreateChangesOnAssocToMany(syncronousMachine, reactiveCapabilityCurves_model, foGost, roGost, reactiveCapabilityCurve_foGost, reactiveCapabilityCurve_roGost);

            var primeMovers_model = metaClassConverted.AllProperties.First(x => x.Name == "PrimeMovers");
            var primeMover_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "PrimeMover");
            var primeMover_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "PrimeMover");

            CreateChangesOnAssocToMany(syncronousMachine, primeMovers_model, foGost, roGost, primeMover_foGost, primeMover_roGost);
        }

        public override bool CanHandle(string className, XElement xmlElement)
        {
            if (className == metaClassToConvert.Name)
            {
                // Проверка существования синхронной машины ВЭС, в которую должна импортироваться асинхронная из СИМ портала
                if (isPartOfWindPlantInModel(new Guid(xmlElement.FirstAttribute.Value.Substring(2))))
                    return true;

                // Если синхронной машины не существует, то проверяем, чтобы тип асинхронной машины был generator
                else if (isPartOfWindPlantInXML(xmlElement))
                    return true;
            }
            return false;
        }

        public override bool CanHandle(DifferenceObject foGost, DifferenceObject roGost)
        {
            // при Description класс объекта не указан, так что надо определить
            if ((foGost ?? roGost)?.IsDescription == true)
            {
                if (isPartOfWindPlantInModel((foGost ?? roGost).ObjectUid))
                    return true;
            }

            if ((foGost ?? roGost)?.ObjectClass?.Name == metaClassToConvert.Name)
            {
                // Проверка существования синхронной машины ВЭС, в которую должна импортироваться асинхронная из СИМ портала
                if (isPartOfWindPlantInModel((foGost ?? roGost).ObjectUid))
                    return true;

                // Если синхронной машины не существует, то проверяем, чтобы тип асинхронной машины был generator в forward секции
                else if (isPartOfWindPlantInDiff(foGost))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Проверяем, есть ли синхронная машина (соотв асинхронной) в модели и принадлежит ли она ВЭС
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        private bool isPartOfWindPlantInModel(Guid guid)
        {
            var obj = modelImage.GetObject<SynchronousMachine>(guid);
            if (obj == null) 
                return false;

            var vl = obj.EquipmentContainer;
            if (vl != null)
            {
                var plant = (vl as VoltageLevel).Substation?.Plant;
                if (plant != null && plant.PSRType == this.psrRType)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Проверяем есть ли данные о том, принадлежит ли асинхронная машина к ВЭС в XML
        /// </summary>
        /// <param name="xmlElement"></param>
        /// <returns></returns>
        private bool isPartOfWindPlantInXML(XElement xmlElement)
        {
            var propsXML = xmlElement.Elements();
            if (propsXML.Any(x => x.FirstAttribute?.Value?.Split('.')?.LastOrDefault() == "generator"))
                return true;
            return false;
        }

        private bool isPartOfWindPlantInDiff(DifferenceObject foGost)
        {
            if (foGost != null)
            {
                var prop = foGost.Properties.FirstOrDefault(x => x.Name == "asynchronousMachineType");
                if (prop != null && foGost.GetEnum(prop as ClassAttribute) == 1)
                    return true;
            }
            return false;
        }
    }


    /// <summary>
    /// PowerElectronicsReactiveCapabilityCurve -> ReactiveCapabilityCurve
    /// </summary>
    public class PowerElectronicsReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import : ConversionStrategyImport
    {
        public PowerElectronicsReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return null; // тк может быть и в СЭС и в ВЭС
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsReactiveCapabilityCurve)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(ReactiveCapabilityCurve)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PowerElectronicsConnection"), metaClassConverted.AllProperties.First(x => x.Name == "InitiallyUsedBySynchronousMachines"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Curve)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<ReactiveCapabilityCurve>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<ReactiveCapabilityCurve>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement)
        {
            var curve = obj as ReactiveCapabilityCurve;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var powerElectronicsConnection_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "PowerElectronicsConnection");

            CreateChangesOnAssocToMany(curve, synchronousMachines_model, powerElectronicsConnection_propXML);
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost)
        {
            var curve = obj as ReactiveCapabilityCurve;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var powerElectronicsConnection_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "PowerElectronicsConnection");
            var powerElectronicsConnection_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "PowerElectronicsConnection");

            CreateChangesOnAssocToMany(curve, synchronousMachines_model, foGost, roGost, powerElectronicsConnection_foGost, powerElectronicsConnection_roGost);
        }
    }


    /// <summary>
    /// AsynchronousMachineReactiveCapabilityCurve -> ReactiveCapabilityCurve
    /// </summary>
    public class AsynchronousMachineReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import : ConversionStrategyImport
    {
        public AsynchronousMachineReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(AsynchronousMachineReactiveCapabilityCurve)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(ReactiveCapabilityCurve)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "AsynchronousMachine"), metaClassConverted.AllProperties.First(x => x.Name == "InitiallyUsedBySynchronousMachines"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Curve)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<ReactiveCapabilityCurve>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<ReactiveCapabilityCurve>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement)
        {
            var curve = obj as ReactiveCapabilityCurve;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var asynchronousMachine_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "AsynchronousMachine");

            CreateChangesOnAssocToMany(curve, synchronousMachines_model, asynchronousMachine_propXML);
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost)
        {
            var curve = obj as ReactiveCapabilityCurve;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var asynchronousMachine_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "AsynchronousMachine");
            var asynchronousMachine_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "AsynchronousMachine");

            CreateChangesOnAssocToMany(curve, synchronousMachines_model, foGost, roGost, asynchronousMachine_foGost, asynchronousMachine_roGost);
        }
    }

    public class WindTurbine_Import : ConversionStrategyImport
    {
        public WindTurbine_Import(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(WindTurbine)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(WindTurbine)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            foreach (var prop in modelImage.MetaData.Classes[nameof(WindTurbine)].AllProperties)
            {
                if (prop.Name == "AsynchronousMachine")
                    mapping.Add(prop, metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines"));
                else
                    mapping.Add(prop, prop);
            }
            return mapping;
        }

        protected override IdentifiedObject GetObject(Guid guid)
        {
            var obj = modelImage.GetObject<WindTurbine>(guid);
            if (obj != null)
                return obj;
            obj = modelImage.CreateObject<WindTurbine>(guid);
            obj.ParentObject = modelImage.GetObjects<BaseObjectRoot>()[0];
            return obj;
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, XElement xmlElement)
        {
            var wt = obj as WindTurbine;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var asynchronousMachine_propXML = xmlElement.Elements().FirstOrDefault(x => x.Name.LocalName.Substring(x.Name.LocalName.LastIndexOf('.') + 1) == "AsynchronousMachine");

            CreateChangesOnAssocToMany(wt, synchronousMachines_model, asynchronousMachine_propXML);
        }

        protected override void CreateAdditionalChangesToObj(IdentifiedObject obj, DifferenceObject foGost, DifferenceObject roGost)
        {
            var wt = obj as WindTurbine;

            var synchronousMachines_model = metaClassConverted.AllProperties.First(x => x.Name == "SynchronousMachines");
            var asynchronousMachine_foGost = foGost?.Properties?.FirstOrDefault(x => x.Name == "AsynchronousMachine");
            var asynchronousMachine_roGost = roGost?.Properties?.FirstOrDefault(x => x.Name == "AsynchronousMachine");

            CreateChangesOnAssocToMany(wt, synchronousMachines_model, foGost, roGost, asynchronousMachine_foGost, asynchronousMachine_roGost);
        }
    }


    /// <summary>
    /// Фабрика стратегий
    /// </summary>
    public class ImportSolarWindStartegyFactory
    {
        private readonly List<IConversionStrategyImport> _strategies;

        public ImportSolarWindStartegyFactory(IModelImage modelImage)
        {
            _strategies = new List<IConversionStrategyImport>
            {
                new PhotoVoltaicUnit_To_ThermalGeneratingUnit_Import(modelImage),
                new PowerElectronicsConnection_To_SynchronousMachine_Import(modelImage),
                new WindGeneratingUnit_To_ThermalGeneratingUnit_Import(modelImage),
                new PowerElectronicsWindUnit_To_ThermalGeneratingUnit_Import(modelImage),
                new AsynchronousMachine_To_SynchronousMachine_Import(modelImage),
                new PowerElectronicsReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import(modelImage),
                new AsynchronousMachineReactiveCapabilityCurve_To_ReactiveCapabilityCurve_Import(modelImage),
                new WindTurbine_Import(modelImage)
            };
        }

        /// <summary>
        /// Выбераем подходящую стратегию
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IConversionStrategyImport GetStrategy(string className, XElement xmlElement)
        {
            return _strategies.FirstOrDefault(s => s.CanHandle(className, xmlElement));
        }

        public IConversionStrategyImport GetStrategy(DifferenceObject foGost, DifferenceObject roGost)
        {
            return _strategies.FirstOrDefault(s => s.CanHandle(foGost, roGost));
        }
    }
}
