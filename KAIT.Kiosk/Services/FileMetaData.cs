
//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.


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
