// Serializer.cs
// class for serializing and deserializing data

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Serializer {
    // int[]
    public byte[] Serialize(int[] content)
    {
        byte[] bytes = new byte[content.Length * 4];

        for (int i = 0; i < content.Length; i++)
        {
            byte[] tmp = BitConverter.GetBytes(content[i]);

            for (int j = 0; j < 4; j++)
                bytes[i * 4 + j] = tmp[j];
        }

        return bytes;
    }

    public int[] DeserializeInt(byte[] bytes)
    {
        int[] content = new int[bytes.Length / 4];
        byte[] _bytes = new byte[4];

        for (int i = 0; i < bytes.Length / 4; i++)
        {
            // reverse endianess
            Array.Copy(bytes, i * 4, _bytes, 0, 4);
            Array.Reverse(_bytes);

            // convert to int
            content[i] = BitConverter.ToInt32(_bytes, 0);
        }

        return content;
    }
}
