using Monitel.Serialization.CIMXML;
using Monitel.Serialization.CIMXML.Providers;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    /// <summary>
    /// Провайдер экспорта в ГОСТ Р 58651
    /// </summary>

#if WITH_FORM
    [DisplayName("ГОСТ Р 58651 (СО ЕЭС) 06.08.2026 с учетом СЭС, ВЭС (испр от 18.09.2026 16:50)")]
#elif WITHOUT_FORM
    [DisplayName("ГОСТ Р 58651 (СО ЕЭС) 06.08.2026 с учетом СЭС, ВЭС")]
#else
    [DisplayName("ГОСТ Р 58651 (СО ЕЭС) 06.08.2026 с учетом СЭС, ВЭС - не использовать (для тестирования)")]
#endif

    [ForceUseMrid]
    [SupportedModel("CIM16")]
    [CanProcessDiffs]
    public class GOSTRdfProvider : BaseRdfProvider
    {
        private readonly BaseRdfProvider _ckProvider = new Monitel.Mal.Context.CIM16.Xml.GOST.GOSTRdfProvider();
        /// <summary>
        /// Имя для отображения
        /// </summary>
        public override string Name => "ГОСТ Р 58651 (СО ЕЭС)";
        /// <summary>
        /// Создаёт провайдер экспорта
        /// </summary>
        public override IExportProvider CreateExportProvider()
        {
            return new GOSTExportProvider(_ckProvider);
        }

        /// <summary>
        /// Создаёт провайдер импорта
        /// </summary>
        public override IImportProvider CreateImportProvider()
        {
            return new GOSTImportProvider(_ckProvider);
        }

        /// <summary>
        /// Поддерживает обработку наборов изменений
        /// </summary>
        public override bool CanProcessDiffs => true;

        /// <summary>
        /// Создает обработчик импортируемого набора изменений
        /// </summary>
        public override IDiffExportProcessor CreateDiffExportProcessor()
        {
            return new DiffExportProcessor(_ckProvider);
        }

        /// <summary>
        /// Создает обработчик импортируемого набора изменений
        /// </summary>
        public override IDiffImportProcessor CreateDiffImportProcessor()
        {
            return new DiffImportProcessor(_ckProvider);
        }
    }
}
