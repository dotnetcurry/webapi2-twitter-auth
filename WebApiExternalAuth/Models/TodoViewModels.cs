using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApiExternalAuth.Models
{
    // Models used by TodoController and TodoListController actions.

    public class TodoItemViewModel
    {
        public TodoItemViewModel() { }

        public TodoItemViewModel(TodoItem item)
        {
            TodoItemId = item.TodoItemId;
            Title = item.Title;
            IsDone = item.IsDone;
            TodoListId = item.TodoListId;
        }

        [Key]
        public int TodoItemId { get; set; }

        [Required]
        public string Title { get; set; }

        public bool IsDone { get; set; }

        public int TodoListId { get; set; }

        public TodoItem ToEntity()
        {
            return new TodoItem
            {
                TodoItemId = TodoItemId,
                Title = Title,
                IsDone = IsDone,
                TodoListId = TodoListId
            };
        }
    }

    public class TodoListViewModel
    {
        public TodoListViewModel() { }

        public TodoListViewModel(TodoList todoList)
        {
            TodoListId = todoList.TodoListId;
            UserId = todoList.UserId;
            Title = todoList.Title;
        }

        [Key]
        public int TodoListId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        public virtual TodoList ToEntity()
        {
            return new TodoList
            {
                Title = Title,
                TodoListId = TodoListId,
                UserId = UserId,
                Todos = new List<TodoItem>()
            };
        }
    }

    public class TodoListDetailsViewModel : TodoListViewModel
    {
        public TodoListDetailsViewModel() { }

        public TodoListDetailsViewModel(TodoList todoList)
            : base(todoList)
        {
            Todos = new List<TodoItemViewModel>();
            foreach (TodoItem item in todoList.Todos)
            {
                Todos.Add(new TodoItemViewModel(item));
            }
        }

        public virtual List<TodoItemViewModel> Todos { get; set; }

        public override TodoList ToEntity()
        {
            TodoList todo = base.ToEntity();
            foreach (TodoItemViewModel item in Todos)
            {
                todo.Todos.Add(item.ToEntity());
            }

            return todo;
        }
    }
}
