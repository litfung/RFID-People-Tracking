using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID_Beta_5
{
    public class TagIds {
        public static String[] recordedTags = new String[] {
         "2222 2222 2222 2222 2222 0006", "2222 2222 2222 2222 2222 0008",
         "2222 2222 2222 2222 2222 0009" ,"2222 2222 2222 2222 2222 000A",
         "2222 2222 2222 2222 2222 000B" ,"2222 2222 2222 2222 2222 000C",
         "2222 2222 2222 2222 2222 000D" ,"2222 2222 2222 2222 2222 000E",
         "2222 2222 2222 2222 2222 000F" ,"2222 2222 2222 2222 2222 0010",
         "2222 2222 2222 2222 2222 0031" ,"2222 2222 2222 2222 2222 0005",
         "2222 2222 2222 2222 2222 0004" ,"2222 2222 2222 2222 2222 0007",
         "2222 2222 2222 2222 2222 0003" ,"2222 2222 2222 2222 2222 0025",
         "2222 2222 2222 2222 2222 0014" ,"2222 2222 2222 2222 2222 0017",
         "2222 2222 2222 2222 2222 0013" ,"2222 2222 2222 2222 2222 0000",
         "2222 2222 2222 2222 2222 0032" ,"2222 2222 2222 2222 2222 001F",
         "2222 2222 2222 2222 2222 0022" ,"2222 2222 2222 2222 2222 0020"
        };

        public static double[] antennaLocationsX = new double[] {84,384,245,233};
        public static double[] antennaLocationsY = new double[] { 216, 224, 82, 309 };

        public static int width=427;
        public static int height = 427;
        public static String[] holdingtags = new String[] {
            "2222 2222 2222 2222 2222 0012"
        };

    }
}
