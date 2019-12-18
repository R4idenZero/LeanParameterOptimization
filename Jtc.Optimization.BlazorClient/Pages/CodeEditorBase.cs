﻿using Blazor.DynamicJavascriptRuntime.Evaluator;
using Blazored.Toast.Services;
using Jtc.Optimization.BlazorClient.Shared;
using Jtc.Optimization.Objects;
using Jtc.Optimization.OnlineOptimizer;
using Jtc.Optimization.Transformation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jtc.Optimization.BlazorClient
{
    public class CodeEditorBase : ComponentBase
    {

        [Inject]
        public IJSRuntime JsRuntime { get; set; }
        [Inject]
        public IToastService ToastService { get; set; }
        protected Models.MinimizeFunctionCode MinimizeFunctionCode { get; set; }
        [CascadingParameter]
        protected EditContext CurrentEditContext { get; set; }
        public string ActivityLog { get { return ActivityLogger?.Output; } }
        public ActivityLogger ActivityLogger { get; set; }
        protected WaitBase Wait { get; set; }

        protected override async Task OnInitializedAsync()
        {
            ActivityLogger = new ActivityLogger(() => StateHasChanged(), (m) => Wait.ShowMessage(m));

            MinimizeFunctionCode = new Models.MinimizeFunctionCode
            {
                Code = Resource.CodeSample
                // Code = "\r\nfunction minimize(p1 /*p2, anything, etc*/)\r\n{\r\n\treturn;\r\n}"
            };

            using (dynamic context = new EvalContext(JsRuntime))
            {
                (context as EvalContext).Expression = () => context.ace.edit("editor").setTheme("ace/theme/monokai");
            }
            using (dynamic context = new EvalContext(JsRuntime))
            {
                (context as EvalContext).Expression = () => context.ace.edit("editor").session.setMode("ace/mode/javascript");
            }

            await base.OnInitializedAsync();
        }

        protected async Task OptimizeClick()
        {

            Wait.Show();

            IterationResult result = null;

            try
            {
                //Console.WriteLine(MinimizeFunctionCode.Code);

                var optimizer = new JavascriptOptimizer(JsRuntime, MinimizeFunctionCode.Code);
                var config = new OptimizerConfiguration
                {
                    Genes = new GeneConfiguration[]
                    {
                    new  GeneConfiguration{ MinDecimal = 0.0m, MaxDecimal = 100m, Precision = 6 },
                    new  GeneConfiguration{ MinDecimal = 0.0m, MaxDecimal = 100m, Precision = 6 }
                    },
                    PopulationSize = 10,
                    Generations = 10000,
                    Fitness = new FitnessConfiguration
                    {
                        OptimizerTypeName = Enums.OptimizerTypeOptions.RandomSearch.ToString()
                    }
                };

                result = await optimizer.Start(config, ActivityLogger);


            }
            catch (Exception ex)
            {
                ToastService.ShowError(ex.Message);
                throw;
            }
            finally
            {
                Wait.Hide();
            }

            ToastService.ShowSuccess("Best Cost:" + result.Error.ToString("N"));
            ToastService.ShowSuccess("Best Parameters:" + string.Join(",", result.ParameterSet.Select(s => s.ToString("N"))));
            ActivityLogger.Add("Best Cost:", result.Error);
            ActivityLogger.Add("Best Parameters:", result.ParameterSet);
        }

    }
}
