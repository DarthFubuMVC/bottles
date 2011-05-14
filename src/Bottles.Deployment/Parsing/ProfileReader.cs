using System;
using System.Collections.Generic;
using System.Linq;
using Bottles.Configuration;
using Bottles.Deployment.Runtime;
using FubuCore;

namespace Bottles.Deployment.Parsing
{
    public class ProfileReader : IProfileReader
    {
        private readonly IRecipeSorter _sorter;
        private readonly DeploymentSettings _settings;
        private readonly IFileSystem _fileSystem;

        public ProfileReader(IRecipeSorter sorter, DeploymentSettings settings, IFileSystem fileSystem)
        {
            _sorter = sorter;
            _fileSystem = fileSystem;
            _settings = settings;
        }

        public DeploymentPlan Read(DeploymentOptions options)
        {
            var environment = EnvironmentSettings.ReadFrom(_settings.EnvironmentFile);

            return Read(options, environment);
        }

        public DeploymentPlan Read(DeploymentOptions options, EnvironmentSettings environment)
        {
            var deploymentPlan = new DeploymentPlan();

            environment.SetRootSetting(_settings.TargetDirectory);

            var profile = readProfile(environment, options);

            //environment smashing has to happen before this

            var recipes = readRecipes(environment, options, profile);

            deploymentPlan.AddRecipes(recipes);

            var hosts = collateHosts(recipes);

            deploymentPlan.SetEnv(environment);

            deploymentPlan.SetProfile(profile);

            //run diagnostics here

            deploymentPlan.CombineOverrides();

            addProfileSettingsToHosts(profile, hosts);

            addEnvironmentSettingsToHosts(environment, hosts);
            
            deploymentPlan.AddHosts(hosts);
            
            return deploymentPlan;
        }

        private Profile readProfile(EnvironmentSettings environment, DeploymentOptions options)
        {
            var profile = new Profile(environment);
            var profileFile = _settings.GetProfile(options.ProfileName);
            _fileSystem.ReadTextFile(profileFile, profile.ReadText);
            return profile;
        }

        private IEnumerable<HostManifest> collateHosts(IEnumerable<Recipe> recipes)
        {
            if (recipes == null || !recipes.Any())
                throw new Exception("Bah! no recipies");
            

            var firstRecipe = recipes.First();
            recipes.Skip(1).Each(firstRecipe.AppendBehind);

            return firstRecipe.Hosts;
        }

        private IEnumerable<Recipe> readRecipes(EnvironmentSettings environment, DeploymentOptions options, Profile profile)
        {
            var recipes = RecipeReader.ReadRecipes(_settings.RecipesDirectory, environment, profile);
            recipes = buildEntireRecipeGraph(profile, options, recipes);
            // TODO -- log which recipes were selected
            recipes = _sorter.Order(recipes);
            return recipes;
        }

        public static IEnumerable<Recipe> buildEntireRecipeGraph(Profile profile, DeploymentOptions options, IEnumerable<Recipe> allRecipesAvailable)
        {
            var recipesToRun = new List<string>();

            recipesToRun.AddRange(profile.Recipes);
            recipesToRun.AddRange(options.RecipeNames);

            var dependencies = new List<string>();

            recipesToRun.Each(r =>
            {
                var rec = allRecipesAvailable.Single(x => x.Name == r);
                dependencies.AddRange(rec.Dependencies);
            });

            recipesToRun.AddRange(dependencies.Distinct());

            return recipesToRun.Distinct().Select(name => allRecipesAvailable.Single(o => o.Name == name));
        }

        private static void addProfileSettingsToHosts(Profile profile, IEnumerable<HostManifest> hosts)
        {
            hosts.Each(host => host.RegisterSettings(profile.DataForHost(host.Name)));
        }

        private static void addEnvironmentSettingsToHosts(EnvironmentSettings environment, IEnumerable<HostManifest> hosts)
        {
            hosts.Each(host => host.RegisterSettings(environment.DataForHost(host.Name)));
        }
    }
}