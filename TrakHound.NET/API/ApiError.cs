﻿// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Text;

using TrakHound.Logging;
using TrakHound.Tools.Web;

namespace TrakHound.API
{
    public static class ApiError
    {

        public static bool ProcessResponse(string response, string label = null)
        {
            if (!string.IsNullOrEmpty(response))
            {
                bool error = false;

                if (response.Length > 2 && response[0] == '(')
                {
                    int i = response.IndexOf(')', 1);
                    if (i >= 0) error = true;
                }

                if (error)
                {
                    Logger.Log(label + " : Error : " + response, LogLineType.Warning);
                    return false;
                }
                else
                {
                    Logger.Log(label + " Successful", LogLineType.Console);
                    return true;
                }
            }

            return false;          
        }

        public static bool ProcessResponse(HTTP.ResponseInfo responseInfo, string label = null)
        {
            if (responseInfo != null && responseInfo.Body != null)
            {
                string s = Encoding.ASCII.GetString(responseInfo.Body);
                return ProcessResponse(s, label);
            }

            return false;
        }

    }
}
