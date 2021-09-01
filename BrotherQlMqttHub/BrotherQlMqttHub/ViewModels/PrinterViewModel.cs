﻿using System;
using System.Collections.Generic;

namespace BrotherQlMqttHub.ViewModels
{
    public class PrinterViewModel
    {
        private readonly Dictionary<int, int> _tags;

        public PrinterViewModel(string serial, bool isOnline, DateTime lastSeen, string model, int errors, string mediaType, int mediaWidth, Dictionary<int, int> tags)
        {
            Serial = serial;
            LastSeen = lastSeen;
            IsOnline = isOnline;
            Model = model;
            Errors = errors;
            MediaType = mediaType;
            MediaWidth = mediaWidth;
            _tags = tags;
        }

        public bool IsOnline { get; }

        public DateTime LastSeen { get; }

        public string Serial { get; }

        public string Model { get; }

        public int Errors { get; }

        public string MediaType { get; }
        public int MediaWidth { get; }

        public IReadOnlyDictionary<int, int> Tags => _tags;

    }
}