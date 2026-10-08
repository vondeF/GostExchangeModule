using Monitel.DataContext.Tools.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Monitel.Mal.Context.CIM16.Names;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO.SolarWindExtensions
{
    public static class PsrTypes
    {
        public static PSRType SolarPlant(IModelImage modelImage) =>
        modelImage.GetObject<PSRType>(Guid.Parse("1000125C-0000-0000-C000-0000006D746C"));

        public static PSRType WindPlant(IModelImage modelImage) =>
            modelImage.GetObject<PSRType>(Guid.Parse("10001256-0000-0000-C000-0000006D746C"));
    }
}
