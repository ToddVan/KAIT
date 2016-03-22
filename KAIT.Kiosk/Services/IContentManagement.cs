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


using KAIT.Common.Services.Messages;
using System;

namespace KAIT.ContentMetaData
{
    public interface IContentManagement<T> where T: ZoneFileMetaData
    {
        bool LoadContents(string contentFolder, Func<T, bool> filter = null);

        bool LoadItemContents(string item, ManipulationStates itemState);

        bool LoadComparisonContent(string item, string item2);



        IFileMetaData MoveNext();

    }
}
