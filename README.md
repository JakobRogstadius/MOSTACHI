# What is this?

This repository contains source code and input data for MOSTACHI - Model for Optimization and Simulation of Traffic And CHarging Infrastructure. MOSTACHI is a free and open-source agent-based simulation tool designed to study interaction effects in time and space between logistics patterns, competing charging infrastructure and cost-minimizing vehicle operators.

Datasets to run simulations of heavy truck traffic on the Swedish road network are currently available in the repository. If you wish to perform studies with the bundled input data, jump ahead and [start running experiments](#run-experiments). 

To study other road networks than the Swedish or other vehicle groups than heavy trucks, new traffic pattern data is required. Follow the steps below to set up the tool for your transport system of interest.

# How do I get this code to run?

The simulation tool is a software program written in C# (.Net). It runs on all the main operating systems, and comes bundled with project files that can be opened in [Visual Studio 2022](https://visualstudio.microsoft.com/vs/community/) or later (a popular programming IDE on Windows) and it does not need any special hardware. If you don't have access to Visual Studio, you can still compile and run the software from the command line, or through another IDE, as long as you have installed the [.Net SDK](https://dotnet.microsoft.com/en-us/download/). The code has been tested with .NET 6.0.

The software has been tested under Windows 10 Enterprise and Ubuntu 20.04 LTS, with .NET 6.0.

## Top level directory structure

The top level directory (also the root of the repository) contains the following subdirectories, some of which come from the repository and some which are created by the build process:

- `mostachi-source` contains the source code of the simulator and is part of the repository.  
- `data` contains two example data sets, `sweden` which is the dataset our results have been generated from, and`short` which is a small data set that can be used for testing and debugging. It is part of the repository.  
- `README.md` is this file, part of the repository.
- `bin` contains the generated programs and is created by the build system (not in the repository).  
- `obj` is created by the build system (not in the repository)  
- `out` is created by the build system (not in the repository)  

If you want to store data in other locations, edit `/mostachi-source/Commons/paths.xml` to match your local file system. By default, it points at the `short` dataset.

## Configuration

There are no command-line arguments to the programs. File paths are specified in `/mostachi-source/Commons/paths.xml`.

Building the code should copy `paths.xml` to the output `bin` directory. If it does not, copy it manually.

## How to build and run the code from Visual Studio 2022 under Windows

Download and install Visual Studio 2022 Community Edition (or later) and the .Net SDK version 6.0 (or later).

Load `MOSTACHI.sln` in Visual Studio. This gives your a list of the different projects included in the solution. Ensure the correct project is selected as start-up project when following the steps below to run experiments.

Edit `/Commons/paths.xml` to match your local directory.

## How to build and run the code from the command line under Linux

If you have installed the .NET SDK for Linux you can use the `dotnet` command to build code and publish the resulting executable (publishing is the .NET equivalent to the `make install` that Linux users are familiar with). The commands will be:

`cd mostachi-source`  
`dotnet publish --use-current-runtime -c Release -o ../bin`

This will give you four executable programs in the `bin` directory:

- `CalculateRoutes`
- `CompressNodeSequences`
- `GenerateDatasets`
- `ScoreInfrastructurePlan`

The `-c Release` option turns on optimization and parallel execution of the code. The programs can then be run from the `bin` directory. The `--use-current-runtime` option says that the program should be run by the same CLR ("C# virtual machine") used for the compile.

There are no arguments to the programs. Instead, they assume that there is a file named `paths.xml` in the current directory that indicates where inputs can be read and where outputs should be written. Building the code also moves the `paths.xml` to the `bin` directory.

# Preparing for running experiments

## Step 1: Create a weighted origin-destination matrix describing traffic data for your region of interest

The simulation tool was originally developed to analyze traffic on the Swedish road network. As input, we have used an origin-destination (OD) matrix output by the Samgods simulation tool, for the 2016 base scenario. This dataset consists of four files (of which only the first three are ever read by the program). For reference, the simulation tool was developed using a dataset of approximately 200,000 routes between approximately 1,000 places, with estimated traffic intensity for each of four truck classes for each route.

Later on, running the simulation can take minutes to days, depending on geographic area and experimental setup. By also preparing truncated versions of `routes.csv` and `routevehicletype.csv` describing smaller OD matrices, you will have a small dataset for testing to facilitate rapid iteration during development. The directory `data/small` gives one such very small dataset that can be used to verify that the installation of code and tools was successful.

`places.csv`:

| place_id | latitude | longitude | name |
|-|-|-|-|
| int | double | double | string |

Note: Our data had the property that all `place_id`s divisible by 100 referred to regions (with their coordinates being the center of the region), while all other `place_id`s referred to exact coordinates.

`routes.csv`:

| route_id | origin_place_id | destination_place_id |
|-|-|-|
| int | place_id | place_id |

`routevehicletype.csv`:

| route_id | vehicletype_id | annualmovements_load | annualmovements_empty | annualtonnes |
|-|-|-|-|
| route_id | vehicletype_id | float | float | float |

`vehicletype.csv`:

| vehicletype_id | name | description |
|-----|------|--------------------|
| 102 | MGV16 | Lorry medium 3.5-16t |
| 103 | MGV24 | Lorry medium 16-24t |
| 104 | HGV40 | Lorry heavy 24-40t |
| 105 | HGV60 | Lorry heavy 40-60t |

## Step 2: Calculate routes for all OD pairs

The next step is to, for each OD pair, calculate one or several likely routes along the road network. The Swedish OD matrix that we have had access to is primarily at municipality resolution, with only a few key locations included, such as major ports and goods terminals. Our dataset also segments the world outside Sweden into regions larger than municipalities. This introduces a problem, as we require that the sum of all traffic on the road network is reasonably accurate, thus we cannot route all traffic to a municipality to the same point. 

### Prepare a density map to sample locations from

***TODO: This data format resulted from a saved Python dictionary and is not very practical for others to use. Clean it up and update README.md to reflect changes.***

A work-around for this issue was developed by sampling ten lat-lon coordinate pairs for each route with the origin and/or destination being a region. Instead of using the center point of a region, new coordinates were sampled at random based on a probability distribution raster with much greater spatial resolution than the size of the regions. The probability distribution should represent locations where goods transported by truck are picked up and dropped of. Lacking such data, we used population density at 1 km spatial resolution as a proxy variable. This probability density data is stored in `population_data.json` with the format:

```
{
    "1234": 
    { 
        "1": 
        { 
            "position": [12.345678, 8.7654321],
            "population": 12
        } 
        "2": ...
    },
    "1235": ...
}
```

where `"1234"` is a `region_id` and `place_id = (region_id - 700000) / 100`.

### Install a routing server

Next, download an [OpenStreetMap](https://download.geofabrik.de/europe.html) dump covering your region of interest. Then [set up an OSRM server](https://hub.docker.com/r/osrm/osrm-backend/) and load it with your OpenStreetMap data. Using the pre-built docker image is very easy. Note that loading all of Europe into the routing engine may require around 60 GB of RAM.

Verify that HTTP calls can be made to the routing server.

### Run CalculateRoutes

Update the values in `paths.xml` to reflect where input and output files should be stored in your local environment. Input files are the above mentioned `places.csv`, `routes.csv`, `routevehicletype.csv` and `population_data.json`.

Compile and run the program `CalculateRoutes`. **This may take a few days to complete and may produce 100s of GB of logs**, thus you may want to start with the smaller test dataset, either the one in the distribution or one of your own making.

This should generate the files `nodesequences.bin.part_#`, `nodes.bin` and `nodepairs.bin`. Any errors encountered during execution will be printed to `error.txt`.

## Step 3: Run CompressNodeSequences

The rather unmanageable output of the previous step contains, for each route, a long sequence of OpenStreetMap `way_id`s that describes the stepwise route along the road network from origin to destination. As OpenStreetMap represents the road network in higher resolution than we need, the data can be made more manageable by simplification of the road network.

Compile and run the program `CompressNodeSequences`. This should generate the files `clusternodepairs.bin`, `nodepairclusters.bin`, `clustersequences.bin` and `bidirectionalnodes.csv` in the data directory specified in `paths.xml`. The space-consuming `nodesequences.bin.part_#` files are no longer needed and can be deleted, unless you wish to experiment with other compression schemes. **Modifying the `CompressNodeSequences` algorithm to simplify the road network further is likely one of the best ways to improve the computational performance of the simulation.**

## Step 4: Pre-calculate several datasets, such as the build-out order for electric roads

Compile and run the program `GenerateDatasets`. This should generate the files `acea_all_stop_location_matches.csv`, `route_length_class.csv`, `cluster_traffic_and_length.csv` and `ers_build_order.csv`.

## Step 5: Modify global parameter values to represent your region of interest

The file `model_parameters.xlsx` contains assumed fuel and electricity prices, vehicle lifetimes, utilization rates by time of day for charging infrastructure, taxes and hundreds of other parameters. Some of these are dependent on geographic location and may need adjustment to better represent your region of interest. It is also possible that the default parameter assumptions do not reflect how the world developed since they were last updated. Once modified, go to the sheet named `code` and copy all highlighted cells in the N column into the file `ScoreInfrastructurePlan/Parameters_PASTE_HERE.cs`.

<a name="run-experiments"></a>
# Define and run experiments to answer research questions

You're all set and it's time to do actual research!

The simulation tool is designed to support comparative experiments, i.e. how does the outcome under conditions A differ from the outcome under conditions B. Each of these sets of conditions is referred as a **scenario** and each set of scenarios to be compared is referred to as an **experiment**. Computing an experiment results in several `.csv` files being generated, which then need to be analyzed to answer the questions the experiment was designed for.

Here are some examples of research questions that can be answered in this way:
- How are cumulative CO<sub>2</sub> emissions from heavy road transport affected by the rate at which depot charging is deployed?
- How does errors in the estimated future cost difference between diesel and electricity affect the forecasted demand for total peak charging power at different fast-charging stations built along the main national motorways?
- What is the system-level return on investment for different charging infrastructure at different stages of the transition? What infrastructure should be prioritized first?
- How does the composition and deployment rate of different types of charging infrastructure affect the growth rate in total demand for vehicle batteries?

Open `Experiments.cs` and `ExperimentSetup.cs` within the `ScoreInfrastructurePlan` project.

`Experiments.cs` defines different experiments. Each function returns a list of scenarios to compute that, when compared, can shed light on how differences in input conditions results in differences in outcome in the transport system.

To conduct sensitivity analysis with regards to the global parameter values, see `ExperimentSetup.SensitivityAnalysisTemplate()`.

`ExperimentSetup.cs` defines which of the many defined experiments to compute. There is no graphical user interface yet (reach out if you wish to add one!) and for now, you will have to make do with commenting and uncommenting different experiments. Once you have ensured that the correct experiments will be computed, recompile and run the `ScoreInfrastructurePlan` program.

All output from the experiments is in the form of .csv files stored in 

# Interpret output data to draw conclusions

## Output files

MOSTACHI generates several files based on each simulated scenario. These are output to the folder specified in `mostachi-source/Commons/paths.xml`, with subfolders corresponding to scenario names.
- `[scenario name]/*.driving_raster.txt` - contains aggregated data for all routes in a 5x5 km raster, for each year and vehicle type,
- `[scenario name]/*.infra_raster.txt`- contains information about the installed capacity and utilization of each charging infrastructure site, for each year,
- `[scenario name]/*.routes.txt` - contains information about cost-minimizing choices made for each route, vehicle type and year,
- `[scenario name]/*.stats.txt` - contains summary statistics for the scenario,
- `run_log_YYYY-MM-DD_HH-MM.stats.txt` - contains all summary statistics appended in one file, if multiple scenarios are run as a batch.

Example output files are provided in `/data/example output`.

***TODO: Provide examples how to interpret results in Python***

## How to visualize output in Excel 3D maps

An easy way to make sense of the driving and infrastructure rasters is to load these into separate worksheets in an Excel document. By selecting all data and inserting a "3D map", the data can be visualized as for instance bars, circles or heatmaps, to answer questions of geographic nature.
