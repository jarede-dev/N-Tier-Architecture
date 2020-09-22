﻿using AutoMapper;
using N_Tier.Application.Exceptions;
using N_Tier.Application.Models.TodoList;
using N_Tier.Core.Entities;
using N_Tier.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace N_Tier.Application.Services.Impl
{
    public class TodoListService : ITodoListService
    {
        private readonly ITodoListRepository _todoListRepository;
        private readonly IMapper _mapper;

        public TodoListService(ITodoListRepository todoListRepository, IMapper mapper)
        {
            _todoListRepository = todoListRepository;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAsync(CreateTodoListModel createTodoListModel)
        {
            var todoList = _mapper.Map<TodoList>(createTodoListModel);
            var addedTodoList = await _todoListRepository.AddAsync(todoList);

            return addedTodoList.Id;
        }

        public async Task DeleteAsync(Guid id)
        {
            var todoList = await _todoListRepository.Get(tl => tl.Id == id);

            if (todoList == null)
                throw new NotFoundException("List does not exist anymore");

            await _todoListRepository.DeleteAsync(todoList);
        }
    }
}