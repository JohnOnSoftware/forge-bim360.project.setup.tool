/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM 'AS IS' AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Autodesk.Forge.BIM360.Serialization
{

    public class CostSegmentValue : Base
    {
        #region Properties

        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("code")]
        public string code { get; set; }

        [JsonProperty("originalCode")]
        public string originalCode { get; set; }

        [JsonProperty("description")]
        public string description { get; set; }

        [JsonProperty("segmentId")]
        public string segmentId { get; set; }

        [JsonProperty("parentId")]
        public string parentId { get; set; }

        [JsonProperty("createdAt")]
        public string createdAt { get; }

        [JsonProperty("updatedAt")]
        public string updatedAt { get; }


        #endregion Properties

        #region Constructors

        public CostSegmentValue()
        {

        } // constructor


        #endregion Constructors

        #region Methods

        #endregion Methods

    } // class
} // namespace
