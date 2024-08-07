﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// FSM transition
    /// </summary>
    public class FSMConnection : Connection
    {
        /// <summary>
        /// A line could contain multiple transitions with different conditions
        /// </summary>
        public DelayableNotificationCollection<Transition> Trans { get; } = new DelayableNotificationCollection<Transition>();
        /// <summary>
        /// ViewModel
        /// </summary>
        public FSMConnectionRenderer FSMRenderer;

        public FSMConnection(Connector from, Connector to) : base(from, to)
        {
            FSMRenderer = Renderer as FSMConnectionRenderer;
            FSMRenderer.FSMOwner = this;
        }

        protected override ConnectionRenderer _CreateRenderer(bool isVertical)
        {
            return new FSMConnectionRenderer();
        }
    }
    /// <summary>
    /// ViewModel of FSM connection
    /// </summary>
    public class FSMConnectionRenderer : ConnectionRenderer
    {
        public FSMConnection FSMOwner { get; set; }
        /// <summary>
        /// For displaying
        /// </summary>
        public string Name
        {
            get
            {
                return string.Format("{0} -> {1}"
                    , FSMOwner.Ctr.From.Owner.ForceGetRenderer.FullName
                    , FSMOwner.Ctr.To.Owner.ForceGetRenderer.FullName);
            }
        }
    }
}
