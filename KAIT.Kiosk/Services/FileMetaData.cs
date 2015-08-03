using KAIT.Common.Services.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.ContentMetaData
{
    public class ZoneFileMetaData : IFileMetaData
    {
        public string ContentPath { get; set; }    // path
        // Content Folder would hold content specific to the tracked item or 
        public string ContentFolder { get; set; }

        public int Age { get; set; }
        public int MaxAge { get; set; }
        public string Gender { get; set; }

    }


    public class ItemFileMetaData : IFileMetaData
    {
        public string ContentPath { get; set; }    // path
        // Content Folder would hold content specific to the tracked item or 
        public string ContentFolder { get; set; }

        //ManipulationStates
        public string ItemState { get; set; }
    }


    public interface IFileMetaData
    {
        string ContentPath { get; set; }    // path
        // Content Folder would hold content specific to the tracked item or 
        string ContentFolder { get; set; }
    }

}
