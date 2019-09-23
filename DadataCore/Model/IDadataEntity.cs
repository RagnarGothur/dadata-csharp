﻿using System;
namespace DadataCore.Model
{
    /// <summary>
    /// DaData data entity (address, phone etc).
    /// </summary>
    public interface IDadataEntity
    {
        StructureType structure_type { get; }
    }
}
