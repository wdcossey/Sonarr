﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace Sonarr.Api.V3
{
    [ApiController]
    public abstract class ProviderControllerBase<TProviderResource, TProvider, TProviderDefinition> : ControllerBase
        where TProviderDefinition : ProviderDefinition, new()
        where TProvider : IProvider
        where TProviderResource : ProviderResource, new()
    {
        private readonly IProviderFactory<TProvider, TProviderDefinition> _providerFactory;
        private readonly ProviderResourceMapper<TProviderResource, TProviderDefinition> _resourceMapper;

        protected ProviderControllerBase(
                IProviderFactory<TProvider, TProviderDefinition> providerFactory,
                ProviderResourceMapper<TProviderResource, TProviderDefinition> resourceMapper)
        {
            _providerFactory = providerFactory;
            _resourceMapper = resourceMapper;

            /*
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Name).Must((v,c) => !_providerFactory.All().Any(p => p.Name == c && p.Id != v.Id)).WithMessage("Should be unique");
            SharedValidator.RuleFor(c => c.Implementation).NotEmpty();
            SharedValidator.RuleFor(c => c.ConfigContract).NotEmpty();

            PostValidator.RuleFor(c => c.Fields).NotNull();*/
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("{id:int}")]
        public IActionResult GetProviderById(int id)
            => Ok(_resourceMapper.ToResource(GetProviderDefinitionById(id)));

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetAll()
        {
            var providerDefinitions = _providerFactory.All().OrderBy(p => p.ImplementationName).ToList();

            var result = new List<TProviderResource>(providerDefinitions.Count);

            foreach (var definition in providerDefinitions)
            {
                _providerFactory.SetProviderCharacteristics(definition);

                result.Add(_resourceMapper.ToResource(definition));
            }

            return Ok(result.OrderBy(p => p.Name).ToList());
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [HttpPost]
        public IActionResult CreateProvider([FromBody] TProviderResource providerResource)
        {
            var providerDefinition = GetDefinition(providerResource, false);

            if (providerDefinition.Enable)
                Test(providerDefinition, false);

            providerDefinition = _providerFactory.Create(providerDefinition);

            return Created(Request.Path, providerDefinition);
        }

        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [HttpPut]
        [HttpPut("{id:int?}")]
        public Task<IActionResult> UpdateProvider(int? id, [FromBody] TProviderResource providerResource, [FromQuery] bool forceSave = false)
        {
            if (id.HasValue && providerResource != null)
                providerResource.Id = id.Value;

            var providerDefinition = GetDefinition(providerResource, false);

            // Only test existing definitions if it is enabled and forceSave isn't set.
            if (providerDefinition.Enable && !(forceSave))
                Test(providerDefinition, false);

            _providerFactory.Update(providerDefinition);

            return Task.FromResult<IActionResult>(Accepted(GetProviderDefinitionById(providerDefinition.Id)));
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete("{id:int}")]
        public void DeleteProvider(int id)
            => _providerFactory.Delete(id);

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("schema")]
        public IActionResult GetTemplates()
        {
            var defaultDefinitions = _providerFactory.GetDefaultDefinitions().OrderBy(p => p.ImplementationName).ToList();

            var result = new List<TProviderResource>(defaultDefinitions.Count());

            foreach (var providerDefinition in defaultDefinitions)
            {
                var providerResource = _resourceMapper.ToResource(providerDefinition);
                var presetDefinitions = _providerFactory.GetPresetDefinitions(providerDefinition);

                providerResource.Presets = presetDefinitions.Select(v =>
                {
                    var presetResource = _resourceMapper.ToResource(v);

                    return presetResource as ProviderResource;
                }).ToList();

                result.Add(providerResource);
            }

            return Ok(result);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("test")]
        public IActionResult Test([FromBody] TProviderResource providerResource)
        {
            var providerDefinition = GetDefinition(providerResource, true);

            Test(providerDefinition, true);

            return Ok("{}");
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("testall")]
        public IActionResult TestAll()
        {
            var providerDefinitions = _providerFactory.All()
                                                      .Where(c => c.Settings.Validate().IsValid && c.Enable)
                                                      .ToList();
            var result = new List<ProviderTestAllResult>();

            foreach (var definition in providerDefinitions)
            {
                var validationResult = _providerFactory.Test(definition);

                result.Add(new ProviderTestAllResult
                           {
                               Id = definition.Id,
                               ValidationFailures = validationResult.Errors.ToList()
                           });
            }

            return StatusCode(
                result.Any(c => !c.IsValid) ? StatusCodes.Status400BadRequest : StatusCodes.Status200OK,
                result);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("action/{actionAction:required:regex(.*)}")]
        public IActionResult RequestAction(string actionAction, [FromBody] TProviderResource providerResource)
        {
            var providerDefinition = GetDefinition(providerResource, true, false);

            var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var data = _providerFactory.RequestAction(providerDefinition, actionAction, query);

            return Ok(data);
        }

        private TProviderDefinition GetProviderDefinitionById(int id)
        {
            var definition = _providerFactory.Get(id);
            _providerFactory.SetProviderCharacteristics(definition);
            return definition;
        }

        private TProviderDefinition GetDefinition(TProviderResource providerResource, bool includeWarnings = false, bool validate = true)
        {
            var definition = _resourceMapper.ToModel(providerResource);

            if (validate)
            {
                Validate(definition, includeWarnings);
            }

            return definition;
        }

        protected virtual void Validate(TProviderDefinition definition, bool includeWarnings)
        {
            var validationResult = definition.Settings.Validate();

            VerifyValidationResult(validationResult, includeWarnings);
        }

        protected virtual void Test(TProviderDefinition definition, bool includeWarnings)
        {
            var validationResult = _providerFactory.Test(definition);

            VerifyValidationResult(validationResult, includeWarnings);
        }

        protected void VerifyValidationResult(ValidationResult validationResult, bool includeWarnings)
        {
            var result = new NzbDroneValidationResult(validationResult.Errors);

            if (includeWarnings && (!result.IsValid || result.HasWarnings))
            {
                throw new ValidationException(result.Failures);
            }

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }
    }
}
