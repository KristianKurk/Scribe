﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class NoteCategory
    {
        public int category_id { get; set; }
        public string category_title { get; set; }
        public string category_parent { get; set; }
    }
}