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
using Newtonsoft.Json;

namespace BimProjectSetupCommon.Workflow
{
    public class CostWorkflow : ThreeLeggedWorkflow
    {
        private static BimCostApi _bimCostApi = null;

        private List<CostSegment> _costSegments = null;

        public CostWorkflow(AppOptions options ) : base(options)
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
        /// 
        /// </summary>
        public void SetupCostTemplateFromCsvProcess()
        {
            try
            {
                DataController._costTemplateTable = CsvReader.ReadDataFromCSV( DataController._costTemplateTable, DataController._options.CostSegmentFilePath );
                Log.Info($"Read data from CSV file at {DataController._options.CostSegmentFilePath}");
                if ( false == _options.TrialRun)
                {
                    _costSegments = GetCostSegmentsFromTable(DataController._costTemplateTable);
                    SetupCostTemplate(_costSegments);
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
        /// 
        /// </summary>
        /// <param name="segmentTable"></param>
        /// <returns></returns>
        protected List<CostSegment> GetCostSegmentsFromTable( DataTable segmentTable )
        {
            if( segmentTable == null )
                return null;

            // sort data table by project_name
            DataView view = segmentTable.DefaultView;
            view.Sort = "project_name desc";
            DataTable sorted = view.ToTable();

            //List<ProjectUser> users = new List<ProjectUser>();
            List<CostSegment> segments = new List<CostSegment>();
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

                var segment = GetSegmentForRow(row);
                if (segment != null)
                {
                    Log.Info($"Read segment + {segment.name}");
                    segments.Add(segment);
                }
            }
            return segments;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private CostSegment GetSegmentForRow(DataRow row)
        {
            CostSegment segment = new CostSegment();
            segment.projectName = Util.GetStringOrNull(row["project_name"]);
            segment.name = Util.GetStringOrNull(row["segment_name"]);
            segment.length = Int32.Parse( Util.GetStringOrNull(row["length"]) );
            segment.type = Util.GetStringOrNull(row["type"]);
            segment.sampleCode = Util.GetStringOrNull(row["sample_code"]);
            //AddServices(segment);

            return segment;
        }

        /// <summary>
        /// Setup Cost Template Segments with the segment list 
        /// </summary>
        /// <param name="segments"></param>
        public void SetupCostTemplate( List<CostSegment> segments)
        {
            if(segments == null || segments.Count == 0)
            {
                Log.Info("segment list is null or no segment in the list");
                return;
            }

            Log.Info($"");
            Log.Info($"Start creating segments to cost budget template...");
            var distinctSegments = segments.Select(segment => segment.name).Distinct();
            foreach (CostSegment segment in segments)
            {
                var dmProject = DataController.DmProjects.FirstOrDefault(p => p.attributes != null && p.attributes.name != null && p.attributes.name.Equals(segment.projectName, StringComparison.InvariantCultureIgnoreCase));
                if(dmProject == null || dmProject.relationships == null || dmProject.relationships.cost == null || dmProject.relationships.cost.data == null || dmProject.relationships.cost.data.id == null)
                {
                    Log.Error("Can not get the cost container id from the project.");
                    return;
                }

                string costContainerId = dmProject.relationships.cost.data.id;
                _bimCostApi.GetBudgetCodeTemplates(costContainerId, out List<CostTemplate> templates);
                if (templates == null || templates.Count != 1)
                {
                    Log.Warn("template of this project is not correct");
                    return;
                }
                Log.Info($"Start to add segments to budget code teamplate: {costContainerId}");

                Log.Info($"- add segment to template ");
                IRestResponse response = _bimCostApi.PostBudgetCodeSegment(dmProject.relationships.cost.data.id, templates[0].id, segment);
                if( response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    Log.Info($"Segment {segment.name} is created");
                }
                else
                {
                    Log.Error($"Segment {segment.name} failed to be created due to {response.ErrorMessage}");
                }
            }
        }
    }
}
