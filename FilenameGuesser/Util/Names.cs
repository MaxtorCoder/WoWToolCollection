using System;
using System.Collections.Generic;
using System.Text;

namespace FilenameGuesser.Util
{
    public static class Names
    {
        public static string GetPathFromName(string modelName)
        {
            switch (modelName)
            {
                case "9mal":
                    return "world/expansion08/doodads/maldraxxus/";

            }

            return string.Empty;
        }
    }
}
