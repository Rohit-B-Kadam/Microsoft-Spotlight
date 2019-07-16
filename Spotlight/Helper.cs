using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace Spotlight
{

    public class DataItem
    {
        public string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
            }

        }

    }

    public class Helper
    {
        public List<string> DriveList;
        public DriveInfo[] allDrives;

        public Helper()
        {
            allDrives = DriveInfo.GetDrives();
            DriveList = new List<string>();
            foreach (DriveInfo d in allDrives)
            {
                DriveList.Add(d.Name); 
            }

        }
 

    }

    
}
