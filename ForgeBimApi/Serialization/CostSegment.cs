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

  public class CostSegment : Base
  {
    #region Properties

    [JsonProperty("id")]
    public string id { get; set; }

    [JsonProperty("projectName")]
    public string projectName { get; set; }

    [JsonProperty("name")]
    public string name { get; set; }

    [JsonProperty("type")]
    public string type { get; set; }

    [JsonProperty("delimiter")]
    public string delimiter { get; set; }

    [JsonProperty("length")]
    public int length { get; set; }

    [JsonProperty("position")]
    public long position { get; set; }

    [JsonProperty("sampleCode")]
    public string sampleCode { get; set; }

    #endregion Properties

    #region Constructors

    public CostSegment()
    {

    } // constructor


    #endregion Constructors

    #region Methods

    #endregion Methods

  } // class
} // namespace
