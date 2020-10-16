﻿using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N_Tier.Api.IntergrationTests.Config;
using N_Tier.Api.IntergrationTests.Helpers;
using N_Tier.Application.Models;
using N_Tier.Application.Models.TodoItem;
using N_Tier.Application.Models.TodoList;
using N_Tier.Core.Entities;
using N_Tier.DataAccess.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace N_Tier.Api.IntergrationTests.Tests
{
    [TestFixture]
    public class TodoItemEndpointTests : BaseOneTimeSetup
    {
        [Test]
        public async Task Create_Should_Add_TodoItem_In_Database()
        {
            // Arrange
            //var _host = await SingletonConfig.Get_hostInstanceAsync();

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();

            context.TodoLists.Add(todoListFromDatabase);

            context.SaveChanges();

            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            var createTodoItemModel = Builder<CreateTodoItemModel>.CreateNew().With(cti => cti.TodoListId = todoListFromDatabase.Id).Build();

            // Act
            var apiResponse = await _client.PostAsync("/api/TodoItems", new JsonContent(createTodoItemModel));

            var items = await context.TodoItems.ToListAsync();

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<Guid>>(await apiResponse.Content.ReadAsStringAsync());
            var todoItemFromDatabase = await context.TodoItems.Where(ti => ti.Id == response.Result).FirstOrDefaultAsync();
            CheckResponse.Succeded(response, 201);
            todoItemFromDatabase.Should().NotBeNull();
            todoItemFromDatabase.Title.Should().Be(createTodoItemModel.Title);
            todoItemFromDatabase.List.Id.Should().Be(todoListFromDatabase.Id);
        }

        [Test]
        public async Task Create_Should_Return_Not_Found_If_Todo_List_Does_Not_Exist_Anymore()
        {
            // Arrange
            //var _host = await SingletonConfig.Get_hostInstanceAsync();

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            var createTodoItemModel = Builder<CreateTodoItemModel>.CreateNew().With(cti => cti.TodoListId = Guid.NewGuid()).Build();

            // Act
            var apiResponse = await _client.PostAsync("/api/TodoItems", new JsonContent(createTodoItemModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
        }

        [Test]
        public async Task Update_Should_Update_Todo_Item_From_Database()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();

            var todoItemFromDatabase = Builder<TodoItem>.CreateNew().With(ti => ti.Id = Guid.NewGuid()).With(ti => ti.CreatedBy = user.Id).Build();

            todoListFromDatabase.Items.Add(todoItemFromDatabase);

            context.TodoLists.Add(todoListFromDatabase);

            context.SaveChanges();

            context = null;

            var updateTodoItemModel = Builder<UpdateTodoItemModel>.CreateNew()
                .With(cti => cti.TodoListId = todoListFromDatabase.Id)
                .With(cti => cti.Title = "UpdateTodoItemTitle")
                .With(cti => cti.Body = "UpdateTodoItemBody").Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/TodoItems/{todoItemFromDatabase.Id}", new JsonContent(updateTodoItemModel));

            // Assert
            context = (await GetNewHostAsync()).Services.GetRequiredService<DatabaseContext>();
            var response = JsonConvert.DeserializeObject<ApiResult<Guid>>(await apiResponse.Content.ReadAsStringAsync());
            var modifiedTodoItemFromDatabase = await context.TodoItems.Where(ti => ti.Id == response.Result).FirstOrDefaultAsync();
            CheckResponse.Succeded(response);
            modifiedTodoItemFromDatabase.Should().NotBeNull();
            modifiedTodoItemFromDatabase.Title.Should().Be(updateTodoItemModel.Title);
            modifiedTodoItemFromDatabase.Body.Should().Be(updateTodoItemModel.Body);
        }

        [Test]
        public async Task Update_Should_Return_NotFound_If_Todo_List_Does_Not_Exist_Anymore()
        {
            // Arrange
            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            var updateTodoItemModel = Builder<UpdateTodoItemModel>.CreateNew().Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/TodoItems/{Guid.NewGuid()}", new JsonContent(updateTodoItemModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
        }

        [Test]
        public async Task Update_Should_Return_NotFound_If_Todo_Item_Does_Not_Exist_Anymore()
        {
            // Arrange
            //var _host = await SingletonConfig.Get_hostInstanceAsync();

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();

            context.TodoLists.Add(todoListFromDatabase);

            context.SaveChanges();

            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            var updateTodoItemModel = Builder<UpdateTodoItemModel>.CreateNew().With(cti => cti.TodoListId = todoListFromDatabase.Id).Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/TodoItems/{Guid.NewGuid()}", new JsonContent(updateTodoItemModel));

            var items = await context.TodoItems.ToListAsync();

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
        }

        [Test]
        public async Task Delete_Should_Delete_Todo_Item_From_Database()
        {
            // Arrange
            //var _host = await SingletonConfig.Get_hostInstanceAsync();

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoItemFromDatabase = Builder<TodoItem>.CreateNew().With(ti => ti.Id = Guid.NewGuid()).With(ti => ti.CreatedBy = user.Id).Build();

            var todoListFromDatabase = Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();

            todoListFromDatabase.Items.Add(todoItemFromDatabase);

            context.TodoLists.Add(todoListFromDatabase);

            context.SaveChanges();

            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            // Act
            var apiResponse = await _client.DeleteAsync($"/api/TodoItems/{todoItemFromDatabase.Id}");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<Guid>>(await apiResponse.Content.ReadAsStringAsync());
            var deletedTodoListFromDatabase = await context.TodoItems.Where(ti => ti.Id == response.Result).FirstOrDefaultAsync();
            CheckResponse.Succeded(response);
            deletedTodoListFromDatabase.Should().BeNull();
        }

        [Test]
        public async Task Delete_Should_Return_NotFound_If_Item_Does_Not_Exist_Anymore()
        {
            // Arrange
            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            // Act
            var apiResponse = await _client.DeleteAsync($"/api/TodoItems/{Guid.NewGuid()}");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
        }

        [Test]
        public async Task GetAllByListId_Should_Return_All_Todo_Items_From_Specific_List()
        {
            // Arrange
            //var _host = await SingletonConfig.Get_hostInstanceAsync();

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();

            todoListFromDatabase.Items.AddRange(Builder<TodoItem>.CreateListOfSize(25).All().With(ti => ti.Id = Guid.NewGuid()).Build());

            var todoListFromAnotherUsers = Builder<TodoList>.CreateListOfSize(10).All()
                .With(tl => tl.Id = Guid.NewGuid())
                .With(tl => tl.CreatedBy = Guid.NewGuid().ToString())
                .Build();

            foreach (var todoList in todoListFromAnotherUsers)
            {
                todoList.Items.AddRange(Builder<TodoItem>.CreateListOfSize(10).All().With(ti => ti.Id = Guid.NewGuid()).Build());
            }

            context.TodoLists.Add(todoListFromDatabase);
            context.TodoLists.AddRange(todoListFromAnotherUsers);

            context.SaveChanges();

            //var _client = await SingletonConfig.GetAuthenticated_clientInstanceAsync();

            // Act
            var apiResponse = await _client.GetAsync($"/api/todoLists/{todoListFromDatabase.Id}/todoItems");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<IEnumerable<TodoItemResponseModel>>>(await apiResponse.Content.ReadAsStringAsync());
            CheckResponse.Succeded(response);
            response.Result.Should().NotBeNullOrEmpty();
            response.Result.Should().HaveCount(25);
            response.Result.Should().BeEquivalentTo(todoListFromDatabase.Items, options => options.Including(tl => tl.Id));
        }
    }
}