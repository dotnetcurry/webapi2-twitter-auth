using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using WebApiExternalAuth.Models;

namespace WebApiExternalAuth.Controllers
{
    [Authorize]
    [RoutePrefix("api/Todo")]
    public class TodoController : ApiController
    {
        private TodoDbContext db = new TodoDbContext();

        // PUT api/Todo/5
        [HttpPut("{id}", RouteName = "TodoItem")]
        public IHttpActionResult PutTodoItem(int id, TodoItemViewModel todoItemDto)
        {
            if (!ModelState.IsValid)
            {
                return Message(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            if (id != todoItemDto.TodoItemId)
            {
                return StatusCode(HttpStatusCode.BadRequest);
            }

            TodoItem todoItem = todoItemDto.ToEntity();
            TodoList todoList = db.TodoLists.Find(todoItem.TodoListId);
            if (todoList == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(todoList.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to modify a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            // Need to detach to avoid duplicate primary key exception when SaveChanges is called
            db.Entry(todoList).State = EntityState.Detached;
            db.Entry(todoItem).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return StatusCode(HttpStatusCode.OK);
        }

        // POST api/Todo
        [HttpPost("")]
        public IHttpActionResult PostTodoItem(TodoItemViewModel todoItemDto)
        {
            if (!ModelState.IsValid)
            {
                return Message(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            TodoList todoList = db.TodoLists.Find(todoItemDto.TodoListId);
            if (todoList == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(todoList.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to add a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            TodoItem todoItem = todoItemDto.ToEntity();

            // Need to detach to avoid loop reference exception during JSON serialization
            db.Entry(todoList).State = EntityState.Detached;
            db.TodoItems.Add(todoItem);
            db.SaveChanges();
            todoItemDto.TodoItemId = todoItem.TodoItemId;

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, todoItemDto);
            response.Headers.Location = new Uri(Url.Link("TodoItem", new { id = todoItemDto.TodoItemId }));
            return Message(response);
        }

        // DELETE api/Todo/5
        [HttpDelete("{id}")]
        public IHttpActionResult DeleteTodoItem(int id)
        {
            TodoItem todoItem = db.TodoItems.Find(id);
            if (todoItem == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(db.Entry(todoItem.TodoList).Entity.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to delete a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            TodoItemViewModel todoItemDto = new TodoItemViewModel(todoItem);
            db.TodoItems.Remove(todoItem);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return Content(HttpStatusCode.OK, todoItemDto);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
