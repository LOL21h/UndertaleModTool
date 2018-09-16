﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib
{
    public class UndertaleSimpleList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    writer.WriteUndertaleObject<T>(this[i]);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleObject<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i+1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertalePointerList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            foreach (T obj in this)
                writer.WriteUndertaleObjectPointer<T>(obj);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    (this[i] as UndertaleObjectWithBlobs)?.SerializeBlobBefore(writer);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing blob for item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    writer.WriteUndertaleObject<T>(this[i]);
                    // The last object does NOT get padding (TODO: at least in AUDO)
                    if (IndexOf(this[i]) != Count - 1)
                        (this[i] as PaddedObject)?.SerializePadding(writer);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleObjectPointer<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
            if (Count > 0 && reader.Position != reader.GetAddressForUndertaleObject(this[0]))
            {
                int skip = (int)reader.GetAddressForUndertaleObject(this[0]) - (int)reader.Position;
                if (skip > 0)
                {
                    //Console.WriteLine("Skip " + skip + " bytes of blobs");
                    reader.Position = reader.Position + (uint)skip;
                }
                else
                    throw new IOException("First list item starts inside the pointer list?!?!");
            }
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    T obj = reader.ReadUndertaleObject<T>();
                    if (!obj.Equals(this[(int)i]))
                        throw new IOException("Something got misaligned...");
                    if (i != count - 1)
                        (obj as PaddedObject)?.UnserializePadding(reader);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }
}
