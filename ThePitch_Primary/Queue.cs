using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThePitch_Primary
{
    public class Queue<T> where T : class
    {
        Queue<T> q = new Queue<T>();

        public void Add(T item)
        {
            q.Add(item);
        }

        public T Dequeue()
        { 
            return q.Dequeue();
        }

        public event EventHandler<T> QueueChanged;  

    }
}
