﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Base;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
}

