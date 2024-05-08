//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Contains events from Object and VAMap providers
    /// </summary>
    public interface IObjectRefTrace
    {
        /// <summary>
        /// Object name if existing. 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Kernel object pointer. The pointer value can be reused.
        /// For file mapping events it contains the file mapping address and the process id as synthetic Object Pointer.
        /// </summary>
        long ObjectPtr { get; }

        /// <summary>
        /// First create event 
        /// </summary>
        IRefCountChangeEvent CreateEvent { get; }

        /// <summary>
        /// Last close event when the object is actually deleted.
        /// </summary>
        IRefCountChangeEvent DestroyEvent { get; }

        /// <summary>
        /// Object lifetime
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// True if object was accessed from multiple processes.
        /// </summary>
        bool IsMultiProcess { get; }

        /// <summary>
        /// If true the object contains only file mapping events
        /// </summary>
        bool IsFileMap { get; } 

        /// <summary>
        /// Contains all handle close events if Handle tracing was enabled.
        /// </summary>
        IReadOnlyList<IHandleCloseEvent> HandleCloseEvents { get; }

        /// <summary>
        /// Contains all handle create events if Handle tracing was enabled.
        /// </summary>
        IReadOnlyList<IHandleCreateEvent> HandleCreateEvents { get; }

        /// <summary>
        /// Contains all handle duplicate events if Handle tracing was enabled.
        /// </summary>
        IReadOnlyList<IHandleDuplicateEvent> HandleDuplicateEvents { get; }


        /// <summary>
        /// Contains all object reference change events if ObjectRef tracing was enabled.
        /// </summary>
        IReadOnlyList<IRefCountChangeEvent> RefChanges { get; }


        /// <summary>
        /// Contains all file mapping events if VAMAP provider was enabled.
        /// </summary>
        IReadOnlyList<IFileMapEvent> FileMapEvents { get; }


        /// <summary>
        /// Contains all file unmapping events if VAMAP provider was enabled.
        /// </summary>
        IReadOnlyList<IFileMapEvent> FileUnmapEvents { get; }
    }
}