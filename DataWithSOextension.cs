using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    static class DataWithSOextension
    {
        //атрибуты, которые должны быть экспортированы с префиксом so
        static public Dictionary<string, string[]> AttributesWithSOextension = new Dictionary<string, string[]>()
        {
            { nameof(AsynchronousMachineInfo), new string[] { "efficiency", "nominalRotationSpeed" , "ratedMechanicalPower" } },
            { nameof(CurrentTransformerWinding),  new string[] { "primaryCurrent" } },
            { nameof(ACLineSeriesSection), new string[] { "isTransposed" } },
            { nameof(RotatingMachineInfo), new string[] { "ratedPowerFactor", "ratedS", "ratedU" } },
            { nameof(ExcitationSystem), new string[] { "isBackup" } }
        };

        //ассоциации, которые должны быть экспортированы с префиксом so
        static public Dictionary<string, string[]> AssocsWithSOextension = new Dictionary<string, string[]>()
        {
            { nameof(Asset), new string[] { "AssetType" } },
            { nameof(AssetType), new string[] { "Assets" } }
        };

        //классы, которые должны быть экспортированы с префиксом so (все атрибуты и ассоциации, если у них нет иного префикса, тоже экспортируются с so)
        static public string[] ClassesWithSOextension = new string[]
        {
            nameof(AssetDataSource),
            nameof(AssetType),
            nameof(AssetSet),
            nameof(PhotoVoltaicUnitInfo),
            nameof(PowerElectronicsUnitInfo),
            nameof(PowerElectronicsConnectionInfo),
            nameof(PowerElectronicsWindUnitInfo)
        };
    }
}
