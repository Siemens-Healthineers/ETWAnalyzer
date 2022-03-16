//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace TAU.Toolkit.Diagnostics.Profiling.Simplified
{
    /// <summary/>
    public class ProfilingConstants
    {
        internal static readonly long MAGIC_DURATION_4_AdjustDurationAndCopyToRemote_CALLED_FROM_DISPOSE = 33666;
        internal static readonly string PAYLOAD_FILENAME_CLIENT_TO_SERVER_TIME_DIFF = "AddThisTimeInMSToClientTimeStampToGetTheCorrespondingServerTime.double";
        internal static readonly string PAYLOAD_FILENAME_SERVER_TO_CLIENT_TIME_DIFF = "AddThisTimeInMSToServerTimeStampToGetTheCorrespondingClientTime.double";
        /// <summary>
        /// Write a marker which starts with this string 
        /// to automatically set the start point of the default zoom when opening to this region in the WPA, when opening with the ProfilingDataManager or the CompareETL.cmd script
        /// you can append any string after this to give your marker more meaning
        /// If you use the ProfilingGuard> ensure that you set the parameter 
        /// writeMarkersForAutoZoom in ProfilingGuard.ProfilingGuard(IUseCaseProfiler, bool) to false
        /// </summary>
        public static readonly string MARKER_NAME_FOR_AUTOZOOM_START = "Start ";
        /// <summary>
        /// Write a marker  which starts with this string 
        /// to automatically set the end point of the default zoom when opening to this region in the WPA, when opening with the ProfilingDataManager or the CompareETL.cmd script
        /// you can append any string after this to give your marker more meaning
        /// If you use the ProfilingGuard> ensure that you set the parameter 
        /// writeMarkersForAutoZoom in ProfilingGuard.ProfilingGuard(IUseCaseProfiler, bool) to false
        /// </summary>
        public static readonly string MARKER_NAME_FOR_AUTOZOOM_STOP = "Stop ";

    }
}
