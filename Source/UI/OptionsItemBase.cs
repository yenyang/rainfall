using ColossalFramework.UI;
using ICities;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rainfall
{
    public abstract class OptionsItemBase
    {
        internal object m_value = default(object);

        /// <summary>
        /// The unique option name. Can't clash with any other option names
        /// or you'll lose data.
        /// </summary>
        public string uniqueName = "";

        /// <summary>
        /// The name that appears on the UI.
        /// </summary>
        public string readableName = "";

        /// <summary>
        /// Whether the option is enabled or not
        /// </summary>
        public bool enabled = false;

        /// <summary>
        /// Create the element on the helper
        /// </summary>
        /// <param name="helper">The UIHelper to attach the element to</param>
        public abstract void Create(UIHelperBase helper);

        internal void IgnoredFunction<T>(T ignored) { }
    }
}
