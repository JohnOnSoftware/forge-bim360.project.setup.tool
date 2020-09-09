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

using Autodesk.Forge.BIM360.Serialization;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Autodesk.Forge.BIM360
{
    public class BimCostApi: ForgeApi
    {

        private string m_ThreeLeggedToken = null;

        public BimCostApi(Token token, ApplicationOptions options) : base(token, options)
        {            
            ContentType = "application/json";
        }

        public String ThreeLeggedToken
        {
            get
            {
                return m_ThreeLeggedToken;
            }
            set
            {
                m_ThreeLeggedToken = value;
            }


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <returns></returns>
        public List<CostTemplate> GetBudgetCodeTemplates(string costContainerId)
        {
            Log.Info($"Querying Cost Template from project '{options.ForgeBimAccountId}'");
            List<CostTemplate> result = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates";
                request.Resource = Urls["cost_templates"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                 IRestResponse response = ExecuteRequest(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    result = JsonConvert.DeserializeObject<List<CostTemplate>>(response.Content, settings);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get template from project {options.ForgeBimAccountId}");
                return null;
            }
            return result;
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public List<CostSegment> GetBudgetCodeSegments(string costContainerId, string templateId )
        {
            int limit = 100;
            Log.Info($"Querying Cost Budget Segments from template '{templateId}'");
            List<CostSegment>  result = new List<CostSegment>();
            CostItemsResponse<CostSegment> segmentsResponse = null;
            int offset = 0;
            do
            {
                segmentsResponse = null;
                try
                {
                    var request = new RestRequest(Method.GET);
                    //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments";
                    request.Resource = Urls["cost_segments"];
                    request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                    request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);

                    request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");
                    request.AddParameter("limit", limit, ParameterType.QueryString);
                    request.AddParameter("offset", offset, ParameterType.QueryString);

                    IRestResponse response = ExecuteRequest(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        segmentsResponse = JsonConvert.DeserializeObject<CostItemsResponse<CostSegment>>(response.Content, settings);
                        result.AddRange(segmentsResponse.results);
                        offset += limit;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to get segments from template {templateId} due to {ex.Message}");
                    return null;
                }
            }while (segmentsResponse != null && segmentsResponse.results != null && segmentsResponse.results.Count == limit);

            return (result.Count==0)? null: result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <param name="templateId"></param>
        /// <param name="segmentId"></param>
        /// <returns></returns>
        public CostSegment GetBudgetCodeSegment(string costContainerId, string templateId, string segmentId )
        {
            Log.Info($"Querying segment details by the id of '{segmentId}'");
            CostSegment segment = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}";
                request.Resource = Urls["cost_segments_segmentId"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");
                IRestResponse response = ExecuteRequest(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    segment = JsonConvert.DeserializeObject<CostSegment>(response.Content, settings);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get segement detail from {segmentId} due to {ex.Message}");
                return null;
            }
            return segment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <param name="templateId"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public bool PostBudgetCodeSegment(string costContainerId, string templateId, CostSegment segment)
        {
            if( costContainerId == null || templateId == null || segment == null)
            {
                Log.Error("The input paramter is null");
                return false;
            }
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.POST);
                //request.Resource = "hq/v1/accounts/{AccountId}/projects";
                request.Resource = Urls["cost_segments"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.DateFormatString = "yyyy-MM-dd";
                settings.NullValueHandling = NullValueHandling.Ignore;
                string segmentString = JsonConvert.SerializeObject(segment, settings);
                request.AddParameter("application/json", segmentString, ParameterType.RequestBody);

                request.AddHeader("content-type", ContentType);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);
            }
            catch ( Exception ex)
            {
                Log.Error($"Segment {segment.name} failed to be created due to {ex.Message}");
                return false;
            }
            return (response.StatusCode == System.Net.HttpStatusCode.Created) ? true : false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <param name="templateId"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public List<CostSegmentValue> GetBudgetCodeSegmentValues(string costContainerId, string templateId, CostSegment segment )
        {
            int limit = 100;
            Log.Info($"Querying Cost budget segment values from segment '{segment.name}'");
            List<CostSegmentValue> result = new List<CostSegmentValue>();
            CostItemsResponse<CostSegmentValue> segmentValuesResponse;
            int offset = 0;
            do
            {
                segmentValuesResponse = null;
                try
                {
                    var request = new RestRequest(Method.GET);
                    //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values";
                    request.Resource = Urls["cost_segments_segmentId_values"];
                    request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                    request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                    request.AddParameter("SegmentId", segment, ParameterType.UrlSegment);

                    request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");
                    request.AddParameter("limit", limit, ParameterType.QueryString);
                    request.AddParameter("offset", offset, ParameterType.QueryString);
                    IRestResponse response = ExecuteRequest(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        segmentValuesResponse = JsonConvert.DeserializeObject<CostItemsResponse<CostSegmentValue>>(response.Content, settings);
                        result.AddRange(segmentValuesResponse.results);
                        offset += limit;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to get values from Segment {segment.name} due to {ex.Message}");
                    return null;
                }
            }while (segmentValuesResponse != null && segmentValuesResponse.results !=null && segmentValuesResponse.results.Count == limit);

            return (result.Count == 0)? null : result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <param name="templateId"></param>
        /// <param name="segmentId"></param>
        /// <param name="segmentValue"></param>
        /// <returns></returns>
        public bool PostBudgetCodeSegmentValue(string costContainerId, string templateId, string segmentId, CostSegmentValue segmentValue)
        {
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.POST);
                //request.Resource = "cost/v1/containers/{ContainerId}/segments/{SegmentId}/values";
                request.Resource = Urls["cost_segments_segmentId_values"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.DateFormatString = "yyyy-MM-dd";
                settings.NullValueHandling = NullValueHandling.Ignore;
                string segmentValueString = JsonConvert.SerializeObject(segmentValue, settings);
                request.AddParameter("application/json", segmentValueString, ParameterType.RequestBody);

                request.AddHeader("content-type", ContentType);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create value for segment {segmentId} due to {ex.Message}");
                return false;
            }
            return (response.StatusCode == System.Net.HttpStatusCode.Created) ? true : false;
        }

        public bool PostBudgetCodeSegmentValuesImport(string costContainerId, string templateId, string segmentId, List<CostSegmentValue> segmentValues)
        {
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.POST);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values:import";
                request.Resource = Urls["cost_segments_segmentId_values_import"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.DateFormatString = "yyyy-MM-dd";
                settings.NullValueHandling = NullValueHandling.Ignore;
                string segmentValueString = JsonConvert.SerializeObject(segmentValues, settings);
                request.AddParameter("application/json", segmentValueString, ParameterType.RequestBody);

                request.AddHeader("content-type", ContentType);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);
            }
            catch ( Exception ex)
            {
                Log.Error($"Failed to import values for segment {segmentId} due to {ex.Message}");
                return false;
            }
            return (response.StatusCode == System.Net.HttpStatusCode.OK) ? true : false;
        }

        public CostSegmentValue GetBudgetCodeSegmentValue(string costContainerId, string templateId, string segmentId, string valueId)
        {
            Log.Info($"Querying Projects from AccountID '{options.ForgeBimAccountId}'");
            CostSegmentValue segmentValue = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values/{ValueId}";
                request.Resource = Urls["cost_segments_values_valueId"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);
                request.AddParameter("ValueId", valueId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                IRestResponse response = ExecuteRequest(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    segmentValue = JsonConvert.DeserializeObject<CostSegmentValue>(response.Content, settings);
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get segement value detail from {valueId} due to {ex.Message}");
                return null;
            }

            return segmentValue;
        }

    }
}
