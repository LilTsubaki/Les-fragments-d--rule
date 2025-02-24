﻿using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using System;
using System.Collections.Generic;

public class NetworkUtils {


    public static void WriteInt(int i, NetworkStream stream)
    {
        byte[] data;
        data = BitConverter.GetBytes(i);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        stream.Write(data,0, sizeof(int));
    }

    public static int ReadInt(NetworkStream stream)
    {
        byte[] data = new byte[sizeof(int)];
       

        stream.Read(data, 0, sizeof(int));
        
        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        return BitConverter.ToInt32(data, 0);
    }

    public static void WriteFloat(float i, NetworkStream stream)
    {
        byte[] data;
        data = BitConverter.GetBytes(i);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        stream.Write(data, 0, sizeof(int));
    }

    public static float ReadFloat(NetworkStream stream)
    {
        byte[] data = new byte[sizeof(float)];


        stream.Read(data, 0, sizeof(float));

        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        return BitConverter.ToSingle(data, 0);
    }

    public static void WriteBool(bool b, NetworkStream stream)
	{
		byte[] data;
		data = BitConverter.GetBytes(b);

		stream.Write(data,0, sizeof(bool));
	}

	public static bool ReadBool(NetworkStream stream)
	{
		byte[] data = new byte[sizeof(bool)];

		stream.Read(data, 0, sizeof(bool));
		return BitConverter.ToBoolean(data, 0);
	}


    public static void WriteChar(char c, NetworkStream stream)
    {
        byte[] data;
        data = BitConverter.GetBytes(c);

        stream.Write(data, 0, sizeof(char));
    }

    public static char ReadChar(NetworkStream stream)
    {
        byte[] data = new byte[sizeof(char)];

        stream.Read(data, 0, sizeof(char));

        return BitConverter.ToChar(data, 0);
    }

    public static void WriteString(string str, NetworkStream stream)
    {
        if (str == null)
        {
            WriteInt(0, stream);
            return;
        }
        WriteInt(str.Length, stream);

        foreach(var c in str)
        {
            WriteChar(c, stream);
        }
    }

    public static string ReadString(NetworkStream stream)
    {
        int length = ReadInt(stream);
        StringWriter sw=new StringWriter();
        for(var i=0; i<length; ++i)
        {
            sw.Write(ReadChar(stream));
        }

        return sw.ToString();

    }

    public static void WriteElement(Element element, NetworkStream stream)
    {
       WriteInt(element._id, stream);
    }

    public static Element ReadElement(NetworkStream stream)
    {
        return Element.GetElement(ReadInt(stream));
    }
     
    public static void WriteRunicBoard(RunicBoard runicBoard, NetworkStream stream)
    {
        WriteInt(runicBoard.RunesOnBoard.Count, stream);
        foreach (KeyValuePair<int, Rune> entry in runicBoard.RunesOnBoard)
        {
            WriteInt(entry.Key, stream);
            WriteElement(entry.Value.Element, stream);
        }
    }

    public static Dictionary<int, Rune> ReadRunicBoard(NetworkStream stream)
    {
        Dictionary<int, Rune> map = new Dictionary<int, Rune>();
        int nbRune = ReadInt(stream);
        for(int i = 0; i<nbRune; ++i)
        {
            int position = ReadInt(stream);
            Rune rune = new Rune(ReadElement(stream), position);
            map.Add(position, rune);
        }

        return map;
    }

    public static void WriteCharacter(Character ch, NetworkStream stream)
    {
        WriteInt(ch._lifeMax, stream);
        WriteInt(ch._lifeCurrent, stream);
        WriteInt(ch.CurrentActionPoints, stream);
        WriteInt(ch.CurrentMovementPoints, stream);
        WriteString(ch.Name, stream);
        WriteInt(ch.TurnNumber, stream);

        WriteInt(ch.Protections.Count, stream);
        foreach (var pro in ch.Protections)
        {
            WriteElement(pro.Key, stream);
            WriteInt(pro.Value, stream);
        }

        WriteInt(ch.ProtectionsNegative.Count, stream);
        foreach (var pro in ch.ProtectionsNegative)
        {
            WriteElement(pro.Key, stream);
            WriteInt(pro.Value, stream);
        }

        /*WriteInt(ch.GlobalProtection, stream);
        WriteInt(ch.GlobalNegativeProtection, stream);*/

        WriteInt(ch.SommeProtection, stream);
        WriteInt(ch.SommeNegativeProtection, stream);

        WriteInt(ch.GlobalShieldValue, stream);

        WriteInt(ch.DamageModifier, stream);
        WriteInt(ch.HealModifier, stream);
        WriteInt(ch.RangeModifier, stream);
        WriteBool(ch.IsCharacterGetDot(), stream);
        WriteBool(ch.IsCharacterGetHot(), stream);

    }

    public static Character ReadCharacter(NetworkStream stream)
    {
        Character ch = new Character(ReadInt(stream));
        ch._lifeCurrent = ReadInt(stream);
        ch.CurrentActionPoints = ReadInt( stream);
        ch.CurrentMovementPoints = ReadInt( stream);
        ch.Name = ReadString(stream);
        ch.TurnNumber = ReadInt(stream);

        int count= ReadInt(stream);
        for (int i=0; i<count; ++i)
        {
            ch.Protections.Add(ReadElement(stream), ReadInt(stream));
        }

        count = ReadInt(stream);
        for (int i = 0; i < count; ++i)
        {
            ch.ProtectionsNegative.Add(ReadElement(stream), ReadInt(stream));
        }

        /*ch.GlobalProtection = ReadInt(stream);
        ch.GlobalNegativeProtection = ReadInt(stream);*/

        ch.SommeProtection = ReadInt(stream);
        ch.SommeNegativeProtection = ReadInt(stream);

        ch.GlobalShieldValue = ReadInt(stream);

        ch.DamageModifier = ReadInt(stream);
        ch.HealModifier = ReadInt(stream);
        ch.RangeModifier = ReadInt(stream);
        ch.GetDot = ReadBool(stream);
        ch.GetHot = ReadBool(stream);

        return ch;
    }

    public static void WriteOrientation(Orientation.EnumOrientation orientation, NetworkStream stream)
    {
        WriteInt((int)orientation, stream);
    }

    public static Orientation.EnumOrientation ReadOrientation(NetworkStream stream)
    {
        return (Orientation.EnumOrientation)ReadInt(stream);
    }

    public static void WriteDirection(Direction.EnumDirection direction, NetworkStream stream)
    {
        WriteInt((int)direction, stream);
    }

    public static Direction.EnumDirection readDirection(NetworkStream stream)
    {
        return (Direction.EnumDirection)ReadInt(stream);
    }

    public static void WriteNode(Node node, NetworkStream stream)
    {
        WriteDirection(node.getDirection(), stream);
        WriteBool(node.NodeUsed, stream);
        WriteInt(node.Nodes.Count, stream);
        foreach(Node n in node.Nodes)
        {
            WriteNode(n, stream);
        }
    }

    public static Node ReadNode(NetworkStream stream)
    {
        Direction.EnumDirection direction = readDirection(stream);
        bool nodeUsed = ReadBool(stream);
        List<Node> nodes = new List<Node>();
        int count = ReadInt(stream);

        for(int i = 0; i < count; i++)
        {
            nodes.Add(ReadNode(stream));
        }

        return new Node(direction, nodeUsed, nodes);
    }

    public static void WriteArea(Area area, NetworkStream stream)
    {
        WriteOrientation(area.Orientation, stream);
        WriteBool(area.RootUsed, stream);
        WriteInt(area.Nodes.Count, stream);
        WriteString(area.ImgName, stream);

        foreach(Node node in area.Nodes)
        {
            WriteNode(node, stream);
        }
    }

    public static Area ReadArea(NetworkStream stream)
    {
        Orientation.EnumOrientation orientation = ReadOrientation(stream);
        bool rootUsed = ReadBool(stream);
        int count = ReadInt(stream);
       
        string imgName = ReadString(stream);

        List<Node> nodes = new List<Node>();

        for (int i = 0; i < count; i++)
        {
            nodes.Add(ReadNode(stream));
        }

        

        return new Area(orientation, rootUsed, nodes);

    }

}
