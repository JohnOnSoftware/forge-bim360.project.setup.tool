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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Autodesk.Forge.BIM360;
using Autodesk.Forge.BIM360.Serialization;
using BimProjectSetupCommon.Helpers;
using RestSharp;

namespace BimProjectSetupCommon.Workflow
{
    public class CostSegmentValueWorkflow : ThreeLeggedWorkflow
    {
        private BimCostApi _bimCostApi = null;

        private List<CostSegmentValue> _costSegmentValues = null;

        public CostSegmentValueWorkflow(AppOptions options ) : base(options)
        {
            DataController.InitializeDmProjects();
        }


        /// <summary>
        /// 
        /// </summary>
        public void prepareData()
        {
            _bimCostApi = new BimCostApi(GetToken, _options);
            _bimCostApi.ThreeLeggedToken = GetToken();
        }


        /// <summary>
        /// Main method to setup cost template from excel file
        /// </summary>
        public void SetupCostSegmentValueFromCsvProcess()
        {
            try
            {
                DataController._costSegmentValueTable = CsvReader.ReadDataFromCSV( DataController._costSegmentValueTable, DataController._options.CostSegmentValueFilePath );
                Log.Info($"Read data from CSV file at {DataController._options.CostSegmentValueFilePath}");
                if ( false == _options.TrialRun)
                {
                    _costSegmentValues = GetCostSegmentValuesFromTable(DataController._costSegmentValueTable);
                    SetupCostSegmentValues(_costSegmentValues);
                }
                else
                {
                    Log.Info("Trial run finished. No further processing");
                }
            }
            catch ( Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segmentValueTable"></param>
        /// <returns></returns>
        protected List<CostSegmentValue> GetCostSegmentValuesFromTable( DataTable segmentValueTable )
        {
            if( segmentValueTable == null )
                return null;

            // sort data table by project_name
            DataView view = segmentValueTable.DefaultView;
            view.Sort = "project_name desc";
            DataTable sorted = view.ToTable();

            List<CostSegmentValue> segmentValues = new List<CostSegmentValue>();
            int i = 0;

            Log.Info($"Start to read {sorted.Rows.Count}  segments");
            // Validate the data and convert
            foreach (DataRow row in sorted.Rows)
            {
                i++;
                string projectName = Util.GetStringOrNull(row["project_name"]);
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    Log.Warn($"No project name provided for row {i} - skipping this line!");
                    continue;
                }

                var segmentValue = GetSegmentValueForRow(row);
                if (segmentValue != null)
                {
                    Log.Info($"Read segmentValue + {segmentValue.code}");
                    segmentValues.Add(segmentValue);
                }
            }
            return segmentValues;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private CostSegmentValue GetSegmentValueForRow(DataRow row)
        {
            CostSegmentValue segmentValue = new CostSegmentValue();
            segmentValue.projectName = Util.GetStringOrNull(row["project_name"]);
            segmentValue.segmentName = Util.GetStringOrNull(row["segmentName"]);
            segmentValue.parentId = Util.GetStringOrNull(row["parentId"]);
            segmentValue.code = Util.GetStringOrNull(row["code"]);
            segmentValue.originalCode = Util.GetStringOrNull(row["originalCode"]);
            segmentValue.description = Util.GetStringOrNull(row["description"]);

            return segmentValue;
        }

        /// <summary>
        /// Setup Cost Template Segments with the segmentValue list 
        /// </summary>
        /// <param name="segments"></param>
        public void SetupCostSegmentValues( List<CostSegmentValue> segmentValues)
        {
            if(segmentValues == null || segmentValues.Count == 0)
            {
                Log.Info("segmentValue list is null or no segmentValue in the list");
                return;
            }

            Log.Info($"");
            Log.Info($"Start creating segmentValues to cost budget template...");
            var distinctSegmentValuess = segmentValues.Select(segmentValue => segmentValue.code).Distinct();
            foreach (CostSegmentValue segmentValue in segmentValues)
            {
                var dmProject = DataController.DmProjects.FirstOrDefault(p => p.attributes != null && p.attributes.name != null && p.attributes.name.Equals(segmentValue.projectName, StringComparison.InvariantCultureIgnoreCase));
                if(dmProject == null || dmProject.relationships == null || dmProject.relationships.cost == null || dmProject.relationships.cost.data == null || dmProject.relationships.cost.data.id == null)
                {
                    Log.Error("Can not get the cost container id from the project.");
                    return;
                }

                string costContainerId = dmProject.relationships.cost.data.id;
                List<CostTemplate>  templates = _bimCostApi.GetBudgetCodeTemplates(costContainerId);
                if (templates == null || templates.Count != 1)
                {
                    Log.Warn("template of this project is not correct");
                    return;
                }

                List<CostSegment> segments = _bimCostApi.GetBudgetCodeSegments(costContainerId, templates[0].id);
                if(segments == null )
                {
                    Log.Warn($"Failed to get segments from template {templates[0].name}");
                    continue;
                }

                var segment = segments.FirstOrDefault(s => s != null && s.name.Equals(segmentValue.segmentName, StringComparison.InvariantCultureIgnoreCase));
                if( segment == null) 
                {
                    Log.Warn($"Failed to add segment code {segmentValue.code} to segment {segmentValue.segmentName}");
                    continue;
                }
                Log.Info($"Start to add segment value {segmentValue.code} to segment: {segment.name}");
                bool ret = _bimCostApi.PostBudgetCodeSegmentValue(dmProject.relationships.cost.data.id, templates[0].id, segment.id, segmentValue);
                if( ret)
                {
                    Log.Info($"Segment code {segmentValue.code} is created");
                }
                else
                {
                    Log.Error($"Segment code {segmentValue.code} failed to be created");
                }
            }
        }
    }
}
