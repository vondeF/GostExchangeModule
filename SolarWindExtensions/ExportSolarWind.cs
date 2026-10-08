using Monitel.DataContext.Tools.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Monitel.Mal.Context.CIM16.Names;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions
{
    public interface IConversionStrategyExport
    {
        // Определяет, может ли стратегия обработать этот объект
        bool CanHandle(IMalObject obj);

        // Возвращает новое имя тега объекта
        string GetElementName();

        // Возвращает новое имя свойства, если оно должно быть преобразовано
        string GetPropertyName(ClassProperty originalProperty);

        // Возвращает пространство имен класса
        string GetElementNamespace();

        // Возвращает пространство имен свойства
        string GetPropertyNamespace(ClassProperty originalProperty);
    }


    public abstract class ConversionStrategyExport : IConversionStrategyExport
    {
        // ИМ
        protected IModelImage modelImage;

        // PSRType СЭС или ВЭС
        protected PSRType psrType;

        // Метасущность класса, из которого необходимо преобразовать
        protected MetaClass metaClassToConvert;

        // Метасущность класса, в который необходимо преобразовать
        protected MetaClass metaClassConverted;

        // Словарь свойств ключ - свойство класса, который нужно экспортировать, значение - свойство класса, в который нужно экспортировать
        protected Dictionary<ClassProperty, ClassProperty> propertyMappings;

        public ConversionStrategyExport(IModelImage modelImage)
        {
            this.modelImage = modelImage;
            psrType = SetPSRType();
            metaClassToConvert = SetMetaClassToConvert();
            metaClassConverted = SetMetaClassConverted();
            propertyMappings = SetPropertyMapping();
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
            while (io?.ParentObject?.Uid != Guid.Parse("00000001-0000-0000-C000-0000006D746C") && io?.ParentObject != null && k < 5)
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

        /// Сеттеры
        protected abstract Dictionary<ClassProperty, ClassProperty> SetPropertyMapping();
        protected abstract PSRType SetPSRType();
        protected abstract MetaClass SetMetaClassToConvert();
        protected abstract MetaClass SetMetaClassConverted();

        /// Реализация интерфейса IConversionStrategyExport
        public virtual bool CanHandle(IMalObject obj) => obj?.MetaType.Name == metaClassToConvert.Name && IsPartOfStationWithPSRType(obj, this.psrType);
        public virtual string GetElementName() => metaClassConverted.Name;
        public virtual string GetPropertyName(ClassProperty originalProperty)
        {
            propertyMappings.TryGetValue(originalProperty, out var newProperty);
            if (newProperty?.Uid == null)
                return "";
            int index = newProperty.Uid.IndexOf(':');
            return newProperty.Uid.Substring(index+1);
            //return $"{this.metaClassConverted.Name}.{newProperty.Name}";
        }
        public static string GetPropertyNameWithoutConvert(ClassProperty property)
        {
            int index = property.Uid.IndexOf(':');
            return property.Uid.Substring(index + 1);
        }
        public virtual string GetElementNamespace()
        {
            int index = metaClassConverted.Uid.IndexOf(':');
            return GetNamespace(metaClassConverted.Uid.Substring(0, index));
        }
        public virtual string GetPropertyNamespace(ClassProperty originalProperty)
        {
            propertyMappings.TryGetValue(originalProperty, out var newProperty);
            int index = newProperty.Uid.IndexOf(':');
            return GetNamespace(newProperty.Uid.Substring(0, index));
        }
        public static string GetPropertyNamespaceWithoutConvert(ClassProperty property)
        {
            int index = property.Uid.IndexOf(':');
            return GetNamespace(property.Uid.Substring(0, index));
        }

        private static string GetNamespace(string namespaceName)
        {
            switch (namespaceName)
            {
                case "cim":
                    return "http://iec.ch/TC57/CIM100#";
                case "rf":
                    return "http://gost.ru/2019/schema-cim01#";
                case "so":
                    return "http://so-ups.ru/2015/schema-cim16#";
                case "me":
                    return "http://monitel.com/2014/schema-cim16#";
            }
            return "http://iec.ch/TC57/CIM100#";
        }
    }

    /// <summary>
    /// ThermalGeneratingUnit -> PhotoVoltaicUnit
    /// </summary>
    public class ThermalGeneratingUnit_To_PhotoVoltaicUnit_Export : ConversionStrategyExport
    {
        public ThermalGeneratingUnit_To_PhotoVoltaicUnit_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.SolarPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(PhotoVoltaicUnit)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "trackerType"), metaClassConverted.AllProperties.First(x => x.Name == "trackerType"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "governorSCD"), metaClassConverted.AllProperties.First(x => x.Name == "governorSCD"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxOperatingP"), metaClassConverted.AllProperties.First(x => x.Name == "maxP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minOperatingP"), metaClassConverted.AllProperties.First(x => x.Name == "minP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "RotatingMachine"), metaClassConverted.AllProperties.First(x => x.Name == "PowerElectronicsConnection"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Equipment)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }
    }

    /// <summary>
    /// ThermalGeneratingUnit -> WindGeneratingUnit
    /// </summary>
    public class ThermalGeneratingUnit_To_WindGeneratingUnit_Export : ConversionStrategyExport
    {
        public ThermalGeneratingUnit_To_WindGeneratingUnit_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(WindGeneratingUnit)];
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

        /// <summary>
        /// Преобразуем в WindGeneratingUnit при отсутствии AssetInfo
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool CanHandle(IMalObject obj)
        {
            var thermalGenUnit = modelImage.GetObject<ThermalGeneratingUnit>(obj.Uid);
            if (obj?.MetaType.Name == metaClassToConvert.Name &&
                IsPartOfStationWithPSRType(obj, this.psrType) &&
                (thermalGenUnit.Assets.Any(x => x.AssetInfo is GeneratingUnitInfo) || 
                    thermalGenUnit.Assets.Count() == 0 || 
                    thermalGenUnit.Assets.All(x => x.AssetInfo == null)))
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// ThermalGeneratingUnit -> PowerElectronicsWindUnit
    /// </summary>
    public class ThermalGeneratingUnit_To_PowerElectronicsWindUnit_Export : ConversionStrategyExport
    {
        public ThermalGeneratingUnit_To_PowerElectronicsWindUnit_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(ThermalGeneratingUnit)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsWindUnit)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "governorSCD"), metaClassConverted.AllProperties.First(x => x.Name == "governorSCD"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxOperatingP"), metaClassConverted.AllProperties.First(x => x.Name == "maxP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minOperatingP"), metaClassConverted.AllProperties.First(x => x.Name == "minP"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "RotatingMachine"), metaClassConverted.AllProperties.First(x => x.Name == "PowerElectronicsConnection"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Equipment)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        public override bool CanHandle(IMalObject obj)
        {
            var thermalGenUnit = modelImage.GetObject<ThermalGeneratingUnit>(obj.Uid);
            if (obj?.MetaType.Name == metaClassToConvert.Name &&
                IsPartOfStationWithPSRType(obj, this.psrType) &&
                thermalGenUnit.Assets.Any(x => x.AssetInfo is PowerElectronicsWindUnitInfo))
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// SynchronousMachine -> PowerElectronicsConnection (Solar)
    /// </summary>
    public class SynchronousMachine_To_PowerElectronicsConnection_SolarExport : ConversionStrategyExport
    {
        public SynchronousMachine_To_PowerElectronicsConnection_SolarExport(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.SolarPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(SynchronousMachine)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsConnection)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "maxQ"), metaClassConverted.AllProperties.First(x => x.Name == "maxQ"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "minQ"), metaClassConverted.AllProperties.First(x => x.Name == "minQ"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedPowerFactor"), metaClassConverted.AllProperties.First(x => x.Name == "ratedPowerFactor"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedS"), metaClassConverted.AllProperties.First(x => x.Name == "ratedS"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "ratedU"), metaClassConverted.AllProperties.First(x => x.Name == "ratedU"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "GeneratingUnit"), metaClassConverted.AllProperties.First(x => x.Name == "PowerElectronicsUnit"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "InitialReactiveCapabilityCurve"), metaClassConverted.AllProperties.First(x => x.Name == "PowerElectronicsReactiveCapabilityCurve"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(RegulatingCondEq)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }
    }

    /// <summary>
    /// SynchronousMachine -> PowerElectronicsConnection (Wind)
    /// </summary>
    public class SynchronousMachine_To_PowerElectronicsConnection_WindExport : SynchronousMachine_To_PowerElectronicsConnection_SolarExport
    {
        public SynchronousMachine_To_PowerElectronicsConnection_WindExport(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        public override bool CanHandle(IMalObject obj)
        {
            var syncMachine = modelImage.GetObject<SynchronousMachine>(obj.Uid);
            if (obj?.MetaType.Name == metaClassToConvert.Name &&
                IsPartOfStationWithPSRType(obj, this.psrType) &&
                syncMachine.Assets.Any(x => x.AssetInfo is PowerElectronicsConnectionInfo))
            {
                return true;
            }
            return false;
        }

    }

    /// <summary>
    /// SynchronousMachine -> AsynchronousMachine
    /// </summary>
    public class SynchronousMachine_To_AsynchronousMachine_Export : ConversionStrategyExport
    {
        public SynchronousMachine_To_AsynchronousMachine_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(SynchronousMachine)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(AsynchronousMachine)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "InitialReactiveCapabilityCurve"), metaClassConverted.AllProperties.First(x => x.Name == "ReactiveCapabilityCurve"));
            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "PrimeMovers"), metaClassConverted.AllProperties.First(x => x.Name == "PrimeMover"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(RotatingMachine)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        public override bool CanHandle(IMalObject obj)
        {
            var syncMachine = modelImage.GetObject<SynchronousMachine>(obj.Uid);
            if (obj?.MetaType.Name == metaClassToConvert.Name && 
                IsPartOfStationWithPSRType(obj, this.psrType) && 
                syncMachine.Assets.Any(x => x.AssetInfo is AsynchronousMachineInfo) )
            {
                return true;
            }
            return false;
        }
        
    }

    /// <summary>
    /// ReactiveCapabilityCurve -> PowerElectronicsReactiveCapabilityCurve
    /// </summary>
    public class ReactiveCapabilityCurve_To_PowerElectronicsReactiveCapabilityCurve_Export : ConversionStrategyExport
    {
        public ReactiveCapabilityCurve_To_PowerElectronicsReactiveCapabilityCurve_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return null;
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(ReactiveCapabilityCurve)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(PowerElectronicsReactiveCapabilityCurve)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "InitiallyUsedBySynchronousMachines"), metaClassConverted.AllProperties.First(x => x.Name == "PowerElectronicsConnection"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Curve)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        public override bool CanHandle(IMalObject obj)
        {
            if (obj?.MetaType.Name == metaClassToConvert.Name 
                && (IsPartOfStationWithPSRType(obj, PsrTypes.SolarPlant(modelImage)) 
                    || IsPartOfStationWithPSRType(obj, PsrTypes.WindPlant(modelImage))))
            {
                var curve = obj as ReactiveCapabilityCurve;
                // Выполняем преобазование, если кривая относится к синхронной машине с PowerElectronicsConnectionInfo
                if (curve.InitiallyUsedBySynchronousMachines.FirstOrDefault()?.Assets.Any(a => a?.AssetInfo is PowerElectronicsConnectionInfo) == true)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// ReactiveCapabilityCurve -> AsynchronousMachineReactiveCapabilityCurve
    /// </summary>
    public class ReactiveCapabilityCurve_To_AsynchronousMachineReactiveCapabilityCurve_Export : ConversionStrategyExport
    {
        public ReactiveCapabilityCurve_To_AsynchronousMachineReactiveCapabilityCurve_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return PsrTypes.WindPlant(modelImage);
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(ReactiveCapabilityCurve)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return modelImage.MetaData.Classes[nameof(AsynchronousMachineReactiveCapabilityCurve)];
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();

            mapping.Add(metaClassToConvert.AllProperties.First(x => x.Name == "InitiallyUsedBySynchronousMachines"), metaClassConverted.AllProperties.First(x => x.Name == "AsynchronousMachine"));
            foreach (var prop in modelImage.MetaData.Classes[nameof(Curve)].AllProperties)
            {
                mapping.Add(prop, prop);
            }
            return mapping;
        }

        public override bool CanHandle(IMalObject obj)
        {
            if (obj?.MetaType.Name == metaClassToConvert.Name && IsPartOfStationWithPSRType(obj, this.psrType))
            {
                var curve = obj as ReactiveCapabilityCurve;
                // Выполняем преобазование, если кривая относится к синхронной машине с AsynchronousMachineInfo
                if (curve.InitiallyUsedBySynchronousMachines.FirstOrDefault()?.Assets.Any(a => a?.AssetInfo is AsynchronousMachineInfo) == true)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Удаление CogenerationPlant
    /// </summary>
    public class CogenerationPlant_Export : ConversionStrategyExport
    {
        public CogenerationPlant_Export(IModelImage modelImage) : base(modelImage) { }

        protected override PSRType SetPSRType()
        {
            return null;
        }

        protected override MetaClass SetMetaClassToConvert()
        {
            return modelImage.MetaData.Classes[nameof(CogenerationPlant)];
        }

        protected override MetaClass SetMetaClassConverted()
        {
            return null;
        }

        protected override Dictionary<ClassProperty, ClassProperty> SetPropertyMapping()
        {
            var mapping = new Dictionary<ClassProperty, ClassProperty>();
            return mapping;
        }

        public override bool CanHandle(IMalObject obj) => obj?.MetaType.Name == metaClassToConvert.Name && (IsPartOfStationWithPSRType(obj, PsrTypes.SolarPlant(modelImage)) || IsPartOfStationWithPSRType(obj, PsrTypes.WindPlant(modelImage)));
    }

    /// <summary>
    /// Преобразование связи с машиной у WindTurbine
    /// </summary>
    public class WindTurbine_Export : ConversionStrategyExport
    {
        public WindTurbine_Export(IModelImage modelImage) : base(modelImage) { }

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
                if (prop.Name == "SynchronousMachines")
                    mapping.Add(prop, metaClassConverted.AllProperties.First(x => x.Name == "AsynchronousMachine"));
                else
                    mapping.Add(prop, prop);
            }
            return mapping;
        }

        public override bool CanHandle(IMalObject obj)
        {
            if (obj?.MetaType.Name == metaClassToConvert.Name && IsPartOfStationWithPSRType(obj, this.psrType))
            {
                var turbine = obj as WindTurbine;
                // Выполняем преобазование, если у связанной синхронной машины есть AsynchronousMachineInfo
                if (turbine.SynchronousMachines.FirstOrDefault()?.Assets.Any(a => a?.AssetInfo is AsynchronousMachineInfo) == true)
                {
                    return true;
                }
            }
            return false;
        }
    }


    /// <summary>
    /// Фабрика стратегий
    /// </summary>
    public class ExportSolarWindStartegyFactory
    {
        private readonly List<IConversionStrategyExport> _strategies;

        public ExportSolarWindStartegyFactory(IModelImage modelImage)
        {
            _strategies = new List<IConversionStrategyExport>
            {
                new ThermalGeneratingUnit_To_PhotoVoltaicUnit_Export(modelImage),
                new ThermalGeneratingUnit_To_WindGeneratingUnit_Export(modelImage),
                new ThermalGeneratingUnit_To_PowerElectronicsWindUnit_Export(modelImage),
                new SynchronousMachine_To_PowerElectronicsConnection_SolarExport(modelImage),
                new SynchronousMachine_To_PowerElectronicsConnection_WindExport(modelImage),
                new SynchronousMachine_To_AsynchronousMachine_Export(modelImage),
                new ReactiveCapabilityCurve_To_PowerElectronicsReactiveCapabilityCurve_Export(modelImage),
                new ReactiveCapabilityCurve_To_AsynchronousMachineReactiveCapabilityCurve_Export(modelImage),
                new CogenerationPlant_Export(modelImage),
                new WindTurbine_Export(modelImage)
            };
        }

        /// <summary>
        /// Выбераем подходящую стратегию
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IConversionStrategyExport GetStrategy(IMalObject obj)
        {
            return _strategies.FirstOrDefault(s => s.CanHandle(obj));
        }
    }
}
