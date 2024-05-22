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
        /// Type id which is mapped to event type name of <see cref="IHandleObjectData.ObjectTypeMap"/>
        /// </summary>
        public UInt16 ObjectType { get; }

        /// <summary>
        /// Existing Handle which belongs to this process
        /// </summary>
        public ETWProcessIndex? ProcessIdx { get; }

        /// <summary>
        /// Existing (possibly leaked) handle which belongs to <see cref="ProcessIdx"/>.
        /// </summary>
        public UInt32? HandleValue { get; }

        /// <summary>
        /// Get Object Type string
        /// </summary>
        /// <param name="extract">Extracted data</param>
        /// <returns>Stringified type string.</returns>
        public string GetObjectType(IETWExtract extract);

        /// <summary>
        /// Object create event from Object Reference Tracing, otherwise the first Handle Create/File Map event can be used <see cref="FirstCreateEvent"/>
        /// </summary>
        IRefCountChangeEvent CreateEvent { get; }

        /// <summary>
        /// Returns CreateEvent or, if no Create Event is existing, the first Handle Create or file map event
        /// </summary>
        IRefCountChangeEvent FirstCreateEvent { get; }

        /// <summary>
        /// Last close event when the object is actually deleted.
        /// </summary>
        IRefCountChangeEvent DestroyEvent { get; }

        /// <summary>
        /// Object destroy event from Object Reference Tracing, otherwise the last handle close or file unmap event is returned.
        /// </summary>
        IRefCountChangeEvent LastDestroyEvent { get; }
        

        /// <summary>
        /// Object lifetime
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// True if object was created/duplicated from multiple processes. If it is false it can still be inherited by 
        /// child processes.
        /// </summary>
        bool IsMultiProcess { get; }

        /// <summary>
        /// If true the object contains only file mapping events
        /// </summary>
        bool IsFileMap { get; } 

        /// <summary>
        /// If true object was accessible at one point in time via two different handles. E.g. via DuplicateHandle or creating a named object twice.
        /// </summary>
        bool IsOverlapped { get; }

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