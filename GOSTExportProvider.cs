using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions;
using Monitel.PlatformInfrastructure;
using Monitel.Serialization.CIMXML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using static Monitel.Mal.Context.CIM16.Names;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    internal class GOSTExportProvider : IExportProvider
    {
        
        Dictionary<string, string[]> attributesWithSOextension = DataWithSOextension.AttributesWithSOextension;
        Dictionary<string, string[]> assocsWithSOextension = DataWithSOextension.AssocsWithSOextension;
        string[] classesWithSOextension = DataWithSOextension.ClassesWithSOextension;

        private Guid docGuid = new Guid("47EF099B-FC30-4DEF-8E1B-673BD7A3A0EC");
        private ExportProviderOptions _options;
        private string _soNs = "http://so-ups.ru/2015/schema-cim16#";
        private string _meNs = "http://monitel.com/2014/schema-cim16#";
        private IModelImage mImage;
        private readonly IExportProvider _ckProvider;
        private XmlWriter _writer;

        /// Принудительное использование поля mRID для заполнения атрибута RDF:About
        public bool ForceUseMridForRfdAbout => _ckProvider.ForceUseMridForRfdAbout;

        private ExportSolarWindStartegyFactory _strategyFactory;
        private IConversionStrategyExport _currentObjectStrategy;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ckRdfProvider"></param>
        public GOSTExportProvider(BaseRdfProvider ckRdfProvider)
        {
            _ckProvider = ckRdfProvider.CreateExportProvider();
        }


        /// <summary>
        /// Запущен экспорт данных
        /// </summary>
        /// <param name="options">Параметры</param>
        /// <returns>Можно ли продолжать экпорт</returns>
        public bool StartExport(ExportProviderOptions options)
        {
            _writer = options.Writer;
            if (!_ckProvider.StartExport(options))
                return false;
            options.Namespaces["so"] = "http://so-ups.ru/2015/schema-cim16#";
            options.Namespaces["rf"] = "http://gost.ru/2019/schema-cim01#";
            options.Namespaces["cim"] = "http://iec.ch/TC57/CIM100#";
            options.Namespaces["me"] = "http://monitel.com/2014/schema-cim16#";
            _options = options;
            mImage = options.ModelImage;
            _strategyFactory = new ExportSolarWindStartegyFactory(mImage);

            return true;
            //return _ckProvider.StartExport(options);
        }



        /// <summary>
        /// Записывает заголовок файла
        /// </summary>
        /// <returns>Использовать ли базовый алгоритм создания заголовка</returns>
        public bool? WritingFileHeader()
        {
            return _ckProvider.WritingFileHeader();
        }



        /// <summary>
        /// Требуется ли экспортировать объект.
        /// Вызывается для объекта-контейнера для определения необходимости экспорта вложенных объектов, не имеющих mRID в режиме экспорта по mRID
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true если требуется экспортировать объект, false если не требуется, null если провайдер не знает про объект</returns>
        public bool? NeedExportObject(IMalObject obj)
        {
            return _ckProvider.NeedExportObject(obj);
        }



        /// <summary>
        /// Начинается экспорт объекта
        /// </summary>
        /// <param name="obj">Экспортируемый объект</param>
        /// <returns>true если требуется экспорт объекта общим алгоритмом, false если не требуется, null если провайдер не знает про объект</returns>
        public bool? StartExportObject(IMalObject obj)
        {
            //копирование phases в phase
            if (obj.MetaType.Name == "Asset")
            {
                Asset asset = mImage.GetObject<Asset>(obj.Uid);
                if (asset.phases != null) { asset.phase = asset.phases; }
            }

            _currentObjectStrategy = _strategyFactory.GetStrategy(obj);
            // Игногрируем CogenerationPlant у СЭС и ВЭС при экспорте
            if (_currentObjectStrategy != null && obj.MetaType.Name == nameof(CogenerationPlant))
            {
                return false;
            }

            return _ckProvider.StartExportObject(obj);
        }
        

        /// <summary>
        /// Начат экспорт свойства
        /// </summary>
        /// <param name="obj">Экспортируемый объект</param>
        /// <param name="property">Экспортируемое свойство</param>
        /// <returns>true если требуется экпорт свойства общим алгоритмом, false если не требуется, null если провайдер не знает про свойство</returns>
        public bool? StartExportProperty(IMalObject obj, ClassProperty property)
        {
            if (property.Domain.Name == nameof(PSRType) && property.Name == nameof(PSRType.className))
            {
                return true;
            }
            // property находится в Domain СО-шного класса, и не имеет префикса rf
            if (classesWithSOextension.Contains(property.Domain.Name) && !property.Uid.StartsWith("rf:"))
            {
                return true;
            }
            if (obj.MetaType.Name == nameof(BreakerInfo) && property.Name == nameof(BreakerInfo.ratedInTransitTime))
            {
                return true;
            }
            if (property.Name == nameof(Asset.Names) && obj.MetaType.IsDescendantOf(mImage.MetaData.Classes["Asset"]))
            {
                return true;
            }
            if (property.Name == nameof(RotatingMachine.ReserveExcitationSystem) && obj.MetaType.IsDescendantOf(mImage.MetaData.Classes["RotatingMachine"]))
            {
                return true;
            }
            if (property.Name == nameof(ExcitationSystem.RotatingMachines) && obj.MetaType.IsDescendantOf(mImage.MetaData.Classes["ExcitationSystem"]))
            {
                return true;
            }
            if (property.Name == nameof(Asset.lifecycleDate) && obj.MetaType.IsDescendantOf(mImage.MetaData.Classes["Asset"]))
            {
                return true;
            }
            if (property.Name == nameof(Asset.phase) && obj.MetaType.IsDescendantOf(mImage.MetaData.Classes["Asset"]))
            {
                return true;
            }
            if (property.Name == nameof(Asset.phases))
            {
                return false;
            }
            //для атрибутов, которые необходимо выгрузить с расширением SO
            if (attributesWithSOextension.ContainsKey(property.Domain.Name))
            {
                if (attributesWithSOextension[property.Domain.Name].Any(x => x == property.Name))
                {
                    return true;
                }
            }
            //для ассоциаций, которые необходимо выгрузить с расширением SO
            if (assocsWithSOextension.ContainsKey(property.Domain.Name))
            {
                if (assocsWithSOextension[property.Domain.Name].Any(x => x == property.Name))
                {
                    return true;
                }
            }

            // Экспорт СЭС и ВЭС (игнорируем свойства, которые не переносятся при преобразовании классов + пропускаем необходимые свойства с расширением so)
             if (_currentObjectStrategy != null)
            {
                var targetName = _currentObjectStrategy.GetPropertyName(property);
                if (targetName == "")
                {
                    return false;
                }
                var propertyNamespace = _currentObjectStrategy.GetPropertyNamespace(property);
                if (propertyNamespace == "http://so-ups.ru/2015/schema-cim16#")
                {
                    return true;
                }
            }

            return _ckProvider.StartExportProperty(obj, property);
        }


        
        /// <summary>
        /// Вызывается перед созданием элемента объекта
        /// </summary>
        /// <param name="obj">Экспортируемый объект</param>
        /// <returns>true если требуется создание элемента объекта общим алгоритмом, false если не требуется, null если провайдер не знает про объект</returns>
        public bool? WritingObjectHeader(IMalObject obj)
        {
            if (classesWithSOextension.Contains(obj?.MetaType.Name))
            {
                _writer.WriteStartElement(obj.MetaType.Name, _soNs);
                return false;
            }

            // Экспорт СЭС и ВЭС (перезаписываем наименование класса)
            if (_currentObjectStrategy != null)
            {
                _writer.WriteStartElement(_currentObjectStrategy.GetElementName(), _currentObjectStrategy.GetElementNamespace());
                return false;
            }

            return _ckProvider.WritingObjectHeader(obj);
        }


        /// <summary>
        /// Проверяем, является ли объект частью станции с выбранным PSRType
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="psrType"></param>
        /// <returns></returns>
        private bool isPartOfStationWithChoosenPSRtype(IMalObject obj, PSRType psrType)
        {
            var uid = obj.Uid;
            var io = mImage.GetObject<IdentifiedObject>(uid);
            int k = 0;

            while (io?.ParentObject?.Uid != Guid.Parse("00000001-0000-0000-C000-0000006D746C") && io?.ParentObject != null && k < 50)
            {
                if (io?.ParentObject is Plant plant)
                {
                    if (plant?.PSRType == psrType)
                    {
                        return true;
                    }
                }
                io = io?.ParentObject;
                k++;
            }
            return false;
        }


        /// <summary>
        /// Вызывается перед созданием элемента свойства
        /// </summary>
        /// <param name="obj">Экспортируемый объект</param>
        /// <param name="property">Экспортируемое свойство</param>
        /// <returns>true если требуется создание элемента свойства общим алгоритмом, false если не требуется, null если провайдер не знает про свойство</returns>
        public bool? WritingPropertyHeader(IMalObject obj, ClassProperty property)
        {
            if (classesWithSOextension.Contains(property.Domain.Name) && !property.Uid.StartsWith("rf:"))
            {
                _writer.WriteStartElement($"{property.Domain.Name}.{property.Name}", _soNs);
                return false;
            }


            //Экспорт СЭС и ВЭС (перезаписываем наименование свойства)
            if (_currentObjectStrategy != null)
            {
                var targetName = _currentObjectStrategy.GetPropertyName(property);
                if (targetName != "")
                {
                    _writer.WriteStartElement(targetName, _currentObjectStrategy.GetPropertyNamespace(property));
                    return false;
                }
            }
            return _ckProvider.WritingPropertyHeader(obj, property);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private string getNamespace(ClassProperty prop)
        {
            return prop.Uid.Split(':')[0];
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private string getAssociationName(IMalObject obj, ClassProperty property)
        {
            return $"{obj.MetaType.Name}.{property.Name}";
        }



        /// <summary>
        /// Вызывается перед завершением элемента объекта
        /// </summary>
        /// <param name="obj"></param>
        public void WritingObjectFooter(IMalObject obj)
        {
            // Для асинхронной машины всегда устанавливаем asynchronousMachineType = generator
            if (_currentObjectStrategy is SynchronousMachine_To_AsynchronousMachine_Export)
            {
                _writer.WriteStartElement("AsynchronousMachine.asynchronousMachineType", "http://iec.ch/TC57/CIM100#");
                _writer.WriteAttributeString("rdf", "resource", "http://www.w3.org/1999/02/22-rdf-syntax-ns#", "cim:AsynchronousMachineKind.generator");
                _writer.WriteEndElement();
            }
            
            _ckProvider.WritingObjectFooter(obj);
        }



        /// <summary>
        /// Завершен экспорта объекта (вызывается вне зависимости от результата StartExportObject)
        /// </summary>
        /// <param name="obj"></param>
        public void EndExportObject(IMalObject obj)
        {
            _ckProvider.EndExportObject(obj);
        }
    }
}
