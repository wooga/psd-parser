﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ntreev.Library.Psd
{
    abstract class ReadableValue<T>
    {
        private readonly PsdReader reader;
        private readonly long position;
        private readonly long length;
        private T value;

        protected ReadableValue(PsdReader reader)
            : this(reader, false)
        {

        }

        protected ReadableValue(PsdReader reader, bool hasLength)
        {
            if (hasLength == true)
            {
                this.length = this.OnLengthGet(reader);
            }

            this.reader = reader;
            this.position = reader.Position;
            this.ReadValue(reader, out this.value);

            reader.Position += this.length;
        }

        public T Value
        {
            get { return this.value; }
        }

        protected virtual long OnLengthGet(PsdReader reader)
        {
            return reader.ReadLength();
        }

        protected abstract void ReadValue(PsdReader reader, out T value);

        protected long Length
        {
            get { return this.length; }
        }
    }
}