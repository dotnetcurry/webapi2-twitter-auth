﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiExternalAuth.Models
{
    /// <summary>
    /// Todo item entity
    /// </summary>
    public class TodoItem
    {
        public int TodoItemId { get; set; }

        [Required]
        public string Title { get; set; }
        public bool IsDone { get; set; }

        [ForeignKey("TodoList")]
        public int TodoListId { get; set; }
        public virtual TodoList TodoList { get; set; }
    }

    /// <summary>
    /// Todo list entity
    /// </summary>
    public class TodoList
    {
        public int TodoListId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        public virtual List<TodoItem> Todos { get; set; }
    }
}
