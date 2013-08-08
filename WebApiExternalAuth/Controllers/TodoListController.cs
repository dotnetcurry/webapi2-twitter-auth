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
    [RoutePrefix("api/TodoList")]
    public class TodoListController : ApiController
    {
        private TodoDbContext db = new TodoDbContext();

        // GET api/TodoList
        [HttpGet("")]
        public IEnumerable<TodoListViewModel> GetTodoListsWithDetails()
        {
            string cachedUserId = User.Identity.GetUserId();
            return db.TodoLists.Include("Todos")
                .Where(u => u.UserId == cachedUserId)
                .OrderByDescending(u => u.TodoListId)
                .AsEnumerable()
                .Select(todoList => new TodoListDetailsViewModel(todoList));
        }

        // GET api/TodoList/5
        [HttpGet("{id}", RouteName = "TodoList")]
        public IHttpActionResult GetTodoList(int id)
        {
            TodoList todoList = db.TodoLists.Find(id);
            if (todoList == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(todoList.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to modify a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            return Content(HttpStatusCode.OK, new TodoListViewModel(todoList));
        }

        // GET api/TodoList/5/details
        [HttpGet("{id}/details")]
        public IHttpActionResult GetTodoListWithDetails(int id)
        {
            TodoList todoList = db.TodoLists.Find(id);
            if (todoList == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(todoList.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to modify a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            return Content(HttpStatusCode.OK, new TodoListDetailsViewModel(todoList));
        }

        // PUT api/TodoList/5
        [HttpPut("{id}")]
        public IHttpActionResult PutTodoList(int id, TodoListViewModel todoListDto)
        {
            if (!ModelState.IsValid)
            {
                return Message(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            if (id != todoListDto.TodoListId)
            {
                return StatusCode(HttpStatusCode.BadRequest);
            }

            TodoList todoList = todoListDto.ToEntity();
            if (!String.Equals(db.Entry(todoList).Entity.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to modify a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            db.Entry(todoList).State = EntityState.Modified;

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

        // POST api/TodoList
        [HttpPost("")]
        public HttpResponseMessage PostTodoList(TodoListViewModel todoListDto)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            todoListDto.UserId = User.Identity.GetUserId();
            TodoList todoList = todoListDto.ToEntity();
            db.TodoLists.Add(todoList);
            db.SaveChanges();
            todoListDto.TodoListId = todoList.TodoListId;

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, todoListDto);
            response.Headers.Location = new Uri(Url.Link("TodoList", new { id = todoListDto.TodoListId }));
            return response;
        }

        // DELETE api/TodoList/5
        [HttpDelete("{id}")]
        public IHttpActionResult DeleteTodoList(int id)
        {
            TodoList todoList = db.TodoLists.Find(id);
            if (todoList == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            if (!String.Equals(db.Entry(todoList).Entity.UserId, User.Identity.GetUserId(), StringComparison.OrdinalIgnoreCase))
            {
                // Trying to delete a record that does not belong to the user
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            TodoListViewModel todoListDto = new TodoListViewModel(todoList);
            db.TodoLists.Remove(todoList);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return Content(HttpStatusCode.OK, todoListDto);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
