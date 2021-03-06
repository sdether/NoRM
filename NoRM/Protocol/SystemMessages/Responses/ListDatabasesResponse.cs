﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoRM.Protocol.SystemMessages.Responses
{
    /// <summary>
    /// The structure for the response to the "listdatabases" command.
    /// </summary>
    public class ListDatabasesResponse
    {
        public double? OK { get; set; }
        public double? TotalSize { get; set; }
        public List<DatabaseInfo> Databases { get; set; }
    }

}
