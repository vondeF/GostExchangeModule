using Monitel.Serialization.CIMXML;
using Monitel.Serialization.CIMXML.Providers;
using System;
using static Monitel.Mal.Context.CIM16.Names;
using System.Data.SqlTypes;
using System.Reflection;
using System.Security.Cryptography;
using Monitel.DataContext.Tools.ModelExtensions;
using System.Linq;
using System.Drawing;
using System.Security.Authentication.ExtendedProtection;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    internal class DiffExportProcessor : IDiffExportProcessor
    {
        private IModelImage _miCim;
        private DifferenceModel _dmCim;
        public string _FileLocation;
        private readonly IDiffExportProcessor _ckProcessor;
        private bool needToExportPhase = false; //при экспорте в наборе изменений создаем phase по phases. Вывод phase по факту изм phases, иначе не выводим, даже если phase изменяется

        /// Записывать расширения СК-11: me:Model.name, md:Model.version в заголовке, me:className в Description
        public bool WriteExtensions => _ckProcessor.WriteExtensions;



        /// <summary>
        /// 
        /// </summary>
        /// <param name="ckRdfProvider"></param>
        public DiffExportProcessor(BaseRdfProvider ckRdfProvider)
        {
            _ckProcessor = ckRdfProvider.CreateDiffExportProcessor();
        }



        /// <summary>
        /// Инициализирует конвертер
        /// </summary>
        /// <param name="dmCim">Исходный набор изменений</param>
        /// <param name="options">Параметры</param>
        /// <returns>Перобразованный набор изменений, который будет записан в CIM XML</returns>
        public DifferenceModel InitProcess(DifferenceModel dmCim, DiffExportProcessorOptions options)
        {
            SimpleLogger.WriteToLogInfo("InitProcess DiffExport started");
            _dmCim = dmCim;
            _miCim = options.SourceModel;
            return _ckProcessor.InitProcess(dmCim, options);
        }

        

        /// <summary>
        /// Проверяет нужно ли копировать объект в преобразованный набор изменений общим алгоритмом
        /// </summary>
        /// <param name="foCim">Объект в forward-секции исходного набора изменений</param>
        /// <param name="roCim">Объект в reverse-секции исходного набора изменений</param>
        /// <param name="mcGost">Класс объекта в преобразованном наборе изменений</param>
        /// <returns>True если объект должен быть скопирован в преобразованный набор изменений</returns>
        public bool CheckObject(DifferenceObject foCim, DifferenceObject roCim, out MetaClass mcGost)
        {
            /*
            //производим запись изменений по phase для Asset только, если меняется phases
            if (foCim?.ObjectClass?.Name == "Asset" || roCim?.ObjectClass?.Name == "Asset")
            {
                ClassProperty phasesProp;
                if (foCim != null) //изм или добавление объекта
                {
                    phasesProp = foCim.Properties.FirstOrDefault(x => x.Name == "phases");
                }
                else //удаление объекта
                {
                    phasesProp = roCim.Properties.FirstOrDefault(x => x.Name == "phases");
                }
                if (phasesProp != null) //если меняется phases
                {
                    needToExportPhase = true;
                    var attrPhases = _dmCim.MetaData.Classes[nameof(Asset)].Attributes.FirstOrDefault(x => x.Name == nameof(Asset.phases));
                    var attrPhase = _dmCim.MetaData.Classes[nameof(Asset)].Attributes.FirstOrDefault(x => x.Name == nameof(Asset.phase));

                    if (foCim != null && roCim != null) //объект изменен
                    {
                        foCim.RemoveEntire(attrPhase); //предварительно удаляем существующую запись об изм phase, если он меняется помимо phases
                        roCim.RemoveEntire(attrPhase);
                        if (foCim.GetEnum(attrPhases as ClassAttribute) != roCim.GetEnum(attrPhases as ClassAttribute)) //обработка ситуации: изм -> выгрузка diff -> без сохранения модели возврат к предыдущему состоянию -> выгрузка diff. Без этого выгрузится одинаковые phases в forward и reverse
                        {
                            int idFo = foCim.GetEnum(attrPhases as ClassAttribute);
                            int idRo = roCim.GetEnum(attrPhases as ClassAttribute);
                            foCim.AddEnum(attrPhase, idFo);
                            roCim.AddEnum(attrPhase, idRo);
                        }
                    }
                    else if (foCim != null && roCim == null) //объект добавлен
                    {
                        int idFo = foCim.GetEnum(attrPhases as ClassAttribute);
                        foCim.RemoveEntire(attrPhase);
                        foCim.AddEnum(attrPhase, idFo);
                    }
                    else if (foCim == null && roCim != null) //объект удален
                    {
                        int idRo = roCim.GetEnum(attrPhases as ClassAttribute);
                        roCim.RemoveEntire(attrPhase);
                        roCim.AddEnum(attrPhase, idRo);
                    }
                }
            }*/
            //try { return _ckProcessor.CheckObject(foCim, roCim, out mcGost); }
            //catch (Exception ex) 
            //{
            //    SimpleLogger.WriteToLogInfo($"OBJECT FORWARD - Uid:{foCim?.ObjectUid.ToString() ?? "null"}, ClassName:{foCim?.ObjectClass?.Name ?? "null"} | REVERSE - Uid:{roCim?.ObjectUid.ToString() ?? "null"}, ClassName:{roCim?.ObjectClass?.Name ?? "null"}");
            //    SimpleLogger.WriteToLogError(ex);
            //    mcGost = null;
            //    return false;
            //}
            return _ckProcessor.CheckObject(foCim, roCim, out mcGost);
        }



        /// <summary>
        /// Проверяет нужно ли копировать значение свойства в преобразованный набор изменений общим алгоритмом
        /// </summary>
        /// <param name="foCim">Объект в forward-секции исходного набора изменений</param>
        /// <param name="roCim">Объект в reverse-секции исходного набора изменений</param>
        /// <param name="cpCim">Свойство в исходном наборе изменений</param>
        /// <param name="foGost">Объект в forward-секции преобразованного набора изменений</param>
        /// <param name="roGost">Объект в reverse-секции преобразованного набора изменений</param>
        /// <param name="cpGost">Свойство объекта в преобразованном наборе изменений</param>
        /// <returns></returns>
        public bool CheckProperty(DifferenceObject foCim, DifferenceObject roCim, ClassProperty cpCim, DifferenceObject foGost, DifferenceObject roGost, out ClassProperty cpGost)
        {
            
            if (foCim?.ObjectClass?.Name == "Asset" || roCim?.ObjectClass?.Name == "Asset")
            {
                if (cpCim.Name == "phases")
                {
                    cpGost = null;
                    return false;
                }

                if (cpCim.Name == "phase" /* && needToExportPhase == false */)
                {
                    cpGost = null;
                    return false;
                }
            }

            if (cpCim.Name == "breakingTime")
            {
                cpGost = null;
                return false;
            }

            //try { return _ckProcessor.CheckProperty(foCim, roCim, cpCim, foGost, roGost, out cpGost); }
            //catch (Exception ex)
            //{
            //    SimpleLogger.WriteToLogInfo($"  PROPERTY Uid:{cpCim?.Uid ?? "null"}");
            //    SimpleLogger.WriteToLogError(ex);
            //    cpGost = null;
            //    return false;
            //}
            return _ckProcessor.CheckProperty(foCim, roCim, cpCim, foGost, roGost, out cpGost);
        }



        /// <summary>
        /// Проверяет должна ли ссылка на объект быть скопирована в преобразованный набор изменений
        /// </summary>
        /// <param name="dsCim">Секция исходного набора изменений</param>
        /// <param name="uid">Идентификатор объекта</param>
        /// <param name="cpGost">Ассоциация в преобразованном наборе изменений</param>
        /// <returns>True если ссылка на объект должна быть скопирована в преобразованный набор изменений</returns>
        public bool CheckLinkedUid(DifferenceSet dsCim, Guid uid, ClassAssociation cpGost)
        {
            return _ckProcessor.CheckLinkedUid(dsCim, uid, cpGost);
        }



        /// <summary>
        /// Завершает процесс преобразования
        /// </summary>
        public void EndProcess()
        {
            //try {
            //    SimpleLogger.WriteToLogInfo("EndProcess DiffExport started");
            //    _ckProcessor.EndProcess();
            //    SimpleLogger.WriteToLogInfo("EndProcess DiffExport ended");
            //}
            //catch (Exception ex) { SimpleLogger.WriteToLogError(ex); }
            _ckProcessor.EndProcess();
        }
    }
}
