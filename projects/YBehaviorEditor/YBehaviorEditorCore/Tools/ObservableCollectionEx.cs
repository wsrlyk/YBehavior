using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.ObjectModel;
using System;

public static class DelayableNotificationCollectionExtension
{
    public static void Sort<T>(this DelayableNotificationCollection<T> observable) where T : IComparable<T>, IEquatable<T>
    {
        List<T> sorted = observable.OrderBy(x => x).ToList();
        for (int i = 0; i < sorted.Count(); i++)
            observable.Move(observable.IndexOf(sorted[i]), i);
    }
}

public class DelayableNotificationCollection<T> : ObservableCollection<T>
{
    #region Types

    public class DelayHandler : IDisposable
    {
        public bool CanNotify { get; set; }
        public Func<bool> NotifyIfTrue { get; set; }

        private DelayableNotificationCollection<T> Collection;

        public DelayHandler(DelayableNotificationCollection<T> collection)
        {
            this.Collection = collection;
            this.CanNotify = true;
        }

        public void ForceNotify()
        {
            this.Collection.ExecBaseOnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Notify()
        {
            // Dummy notification
            this.Collection.ExecBaseOnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Dispose()
        {
            if (this.NotifyIfTrue != null)
            {
                if (this.NotifyIfTrue())
                    this.Notify();
            }
            else
                this.Notify();

            this.CanNotify = true;
            this.NotifyIfTrue = null;
        }
    }

    #endregion

    private DelayHandler Handler;

    public DelayableNotificationCollection()
    {
        this.Handler = new DelayHandler(this);
    }

    private void ExecBaseOnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (this.Handler.CanNotify)
            this.ExecBaseOnCollectionChanged(e);
    }

    public DelayHandler Delay(Func<bool> notifyIfTrue = null)
    {
        this.Handler.CanNotify = false;
        this.Handler.NotifyIfTrue = notifyIfTrue;
        return this.Handler;
    }
}


