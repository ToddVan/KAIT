using KAIT.Common.Services.Messages;
using System;
using System.Collections.Generic;

namespace KAIT.ContentMetaData
{
    public interface IContentManagement<T> where T: ZoneFileMetaData
    {
        bool LoadContents(string contentFolder, Func<T, bool> filter = null);

        bool LoadItemContents(string item, ManipulationStates itemState);

        IFileMetaData MoveNext();

    }
}
