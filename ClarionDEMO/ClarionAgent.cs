﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Clarion;
using Clarion.Framework;
using Clarion.Framework.Extensions;
using Clarion.Framework.Templates;
using WorldServerLibrary.Model;
using WorldServerLibrary;
using System.Threading;
using Gtk;
using System.IO;


namespace ClarionDEMO
{
    /// <summary>
    /// Public enum that represents all possibilities of agent actions
    /// </summary>
    public enum CreatureActions
    {
        GO_BACK   ,
        ROTATE_RIGHT ,
        ROTATE_LEFT  ,
        GO_AHEAD     ,
        GET_ITEM     ,
        EAT_FOOD
    }

    public class ClarionAgent
    {

        #region creature state Memory

        String creatureId = String.Empty;
        String creatureName = String.Empty;
        Creature mycreature;
        private double currentFUEL, thelastFUEL; 
        private int currentLeaf, thelastLeaf;
        bool currentObjTouch, thelastObjTouch;
        bool currentFoodInFront, currentGemInFront;
        bool currentFoodInSide, currentGemInSide;
        bool thelastFoodInFront, thelastGemInFront;
        bool thelastFoodInSide,  thelastGemInSide;
        Thing thingToEat;
        Thing thingToGet;
        string currentAction="";
        double currentFeedBack;
        public int[] leaflet1 = new int[6] { 0, 0, 0, 0, 0, 0};

        #endregion

        #region Constants

        //Setting up visual dimension LABELS
        static private String DIMENSION_SENSOR_VISUAL = "VisualSensor";
        //Setting up Goals dimension LABELS
        static private String DIMENSION_GOALS = "Goals";
        //Setting up visual dimension LABELS
        static private String VALUE_WALL_Ahead    = "Wall Ahead";   
        static private String VALUE_GEM_Ahed      = "Gem Ahead";
        static private String VALUE_FOOD_Ahead    = "Food Ahead";
        static private String VALUE_LLGEM_upRight = "Gem Up Right";
        static private String VALUE_LLGEM_upLeft  = "Gem Up Left";
        static private String VALUE_LLGEM_upFront = "Gem Up Front";
        static private String VALUE_FOOD_upRight  = "Food Up Right";
        static private String VALUE_FOOD_upLeft   = "Food Up Left";
        static private String VALUE_FOOD_upFront  = "Food Up Front";
        //Setting up goals LABELS
        static private String SearchFood = "Search Food";
        static private String SearchGem = "Search Gem";
        double prad = 0;

        #endregion

        #region Execution Parameters

        public double creatureLife=1;
        /// If this value is greater than zero, the agent will have a finite number of cognitive cycle. Otherwise, it will have infinite cycles.
        public double MaxNumberOfCognitiveCycles = -1;
        /// Current cognitive cycle number
        private double CurrentCognitiveCycle = 0;
        private double TotalCognitiveCycle = 0;
        private int completedLeaflets = 0;
        /// Time between cognitive cycle in miliseconds
        public Int32 TimeBetweenCognitiveCycles = 600;
        /// A thread Class that will handle the simulation process
        private Thread runThread;

        // Declaring the world
		private WorldServer worldServer;
 
        // Declaring the agent
        public Clarion.Framework.Agent CurrentAgent;

        //declaring inputs
		private DimensionValuePair inputWallAhead    ;
        private DimensionValuePair inputGemAhead     ;
        private DimensionValuePair inputFoodAhead    ;
        private DimensionValuePair inputGemUpRight   ;
        private DimensionValuePair inputGemUpLeft    ;
        private DimensionValuePair inputGemUpFront   ;
        private DimensionValuePair inputFoodUpRight  ;
        private DimensionValuePair inputFoodUpLeft   ;
        private DimensionValuePair inputFoodUpFront  ;

        //declaring Actions
        private ExternalActionChunk eacDO_NOTHING         ;
        private ExternalActionChunk eacROTATE_RIGHT       ;
        private ExternalActionChunk eacROTATE_LEFT        ;
        private ExternalActionChunk eacGO_AHEAD           ;
        private ExternalActionChunk eacGET_ITEM           ;
        private ExternalActionChunk eacEAT_FOOD           ;

        //declaring goals
        private GoalChunk gSearchFood;
        private GoalChunk gSearchGems;

        #endregion

        /// <summary>
        /// Clarion Agent Constructor
        /// </summary>
        /// <param name="nws"></param>
        /// <param name="creature_ID"></param>
        /// <param name="creature_Name"></param>
        public ClarionAgent(WorldServer nws, String creature_ID, String creature_Name)
        {
            //Resetting leaflet
            leaflet1 = new int[6]{ 0, 0, 0, 0, 0, 0};

            //generating 3 random gem/jewels at leaflet
            Random rand = new Random(DateTime.Now.Millisecond);
            leaflet1[rand.Next(0, 5)]=3;
            //leaflet1[rand.Next(0, 5)]++;
            //leaflet1[rand.Next(0, 5)]++;

            //recording world and creature references
            worldServer = nws;
            CurrentAgent = World.NewAgent("CurrentAgent");
			creatureId = creature_ID;
			creatureName = creature_Name;

            // Initialize Input Information
            inputWallAhead   = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_WALL_Ahead    );
            inputGemAhead    = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_GEM_Ahed      );
            inputFoodAhead   = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_FOOD_Ahead    );
            inputGemUpRight  = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_LLGEM_upRight );
            inputGemUpLeft   = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_LLGEM_upLeft  );
            inputGemUpFront  = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_LLGEM_upFront );
            inputFoodUpRight = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_FOOD_upRight  );
            inputFoodUpLeft  = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_FOOD_upLeft   );
            inputFoodUpFront = World.NewDimensionValuePair(DIMENSION_SENSOR_VISUAL, VALUE_FOOD_upFront  );

            // Initialize Output actions
            eacDO_NOTHING   = World.NewExternalActionChunk(CreatureActions.GO_BACK   .ToString());
            eacROTATE_RIGHT = World.NewExternalActionChunk(CreatureActions.ROTATE_RIGHT .ToString());
            eacROTATE_LEFT  = World.NewExternalActionChunk(CreatureActions.ROTATE_LEFT  .ToString());
            eacGO_AHEAD     = World.NewExternalActionChunk(CreatureActions.GO_AHEAD     .ToString());
            eacGET_ITEM     = World.NewExternalActionChunk(CreatureActions.GET_ITEM     .ToString());
            eacEAT_FOOD     = World.NewExternalActionChunk(CreatureActions.EAT_FOOD     .ToString());

            //initializes goal chunks
            gSearchFood = World.NewGoalChunk(SearchFood);
            gSearchGems = World.NewGoalChunk(SearchGem);
            
            //Create thread to run simulation
            runThread = new Thread(CognitiveCycle);
			Console.WriteLine("Agent started");
        }

        /// <summary>
        /// Run the Simulation in World Server 3d Environment
        /// </summary>
        public void Run()
        {                
			Console.WriteLine ("Running ...");
            // Setup Agent to run
            if (runThread != null && !runThread.IsAlive)
            {
                SetupAgentInfraStructure();
				// Start Simulation Thread                
                runThread.Start(null);
            }
        }

        /// <summary>
        /// Updates the console display information
        /// </summary>
        public void updateConsole()
        {
            Console.Clear();
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            
            Console.WriteLine("---Remaining Leaflet---");
            Console.WriteLine("|   RED="+leaflet1[0]+ "    GREEN=" + leaflet1[1] + "  |");
            Console.WriteLine("|   BLUE=" + leaflet1[2] + "   YELLOW=" + leaflet1[3] + " |");
            Console.WriteLine("|   PURP=" + leaflet1[4] + "   WHITE=" + leaflet1[5] + "  |");
            Console.WriteLine("-----------------------");
            Console.WriteLine("FUEL: "+currentFUEL);
            Console.WriteLine("Creature Life: " + creatureLife);
            Console.WriteLine("Completed Leaflets: " + completedLeaflets);
            Console.WriteLine("Cycle: "+ TotalCognitiveCycle);
            Console.WriteLine("Choosen action: " + currentAction);
            Console.WriteLine("Goal: " + CurrentAgent.CurrentGoal.LabelAsIComparable);
            Console.WriteLine("FeedBack given: " + currentFeedBack);
            //Console.WriteLine("Learned rules:");
            //foreach (var i in CurrentAgent.GetInternals(Agent.InternalContainers.ACTION_RULES))
            //{
            //    Console.WriteLine("\r\n" + i.ToString());
            //}
            

        }

        /// <summary>
        /// Update leaflet with info from server and return total remaining
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public int updateLeaflet(Creature c)
        {
            //leaflet1[0] = 0;
            //leaflet1[1] = 0;
            //leaflet1[2] = 0;
            //leaflet1[3] = 0;
            //leaflet1[4] = 0;
            //leaflet1[5] = 0;
            //foreach (var item in c.getLeaflets())
            //{
            //    leaflet1[0] = leaflet1[0] + c.getLeaflets().First().getRequired("Red");
            //    leaflet1[1] = leaflet1[1] + c.getLeaflets().First().getRequired("Green");
            //    leaflet1[2] = leaflet1[2] + c.getLeaflets().First().getRequired("Blue");
            //    leaflet1[3] = leaflet1[3] + c.getLeaflets().First().getRequired("Yellow");
            //    leaflet1[4] = leaflet1[4] + c.getLeaflets().First().getRequired("Magenta");
            //    leaflet1[5] = leaflet1[5] + c.getLeaflets().First().getRequired("White");
            //}
            
            int result = 0;
            foreach (var item in leaflet1)
            {
                result = result + item;
            }
            return result;
        }

        /// <summary>
        /// Abort the current Simulation
        /// </summary>
        /// <param name="deleteAgent">If true beyond abort the current simulation it will die the agent.</param>
        public void Abort(Boolean deleteAgent)
        {   Console.WriteLine ("Aborting ...");
            if (runThread != null && runThread.IsAlive)
            {
                runThread.Abort();
            }

            if (CurrentAgent != null && deleteAgent)
            {
                CurrentAgent.Die();
            }
        }

        /// <summary>
        /// Returns a list of THINGs seen by the creature
        /// </summary>
        /// <returns></returns>
		IList<Thing> processSensoryInformation()
		{
			IList<Thing> response = null;

			if (worldServer != null && worldServer.IsConnected)
			{
				response = worldServer.SendGetCreatureState(creatureName);
				prad = (Math.PI / 180) * response.First().Pitch;
				while (prad > Math.PI) prad -= 2 * Math.PI;
				while (prad < - Math.PI) prad += 2 * Math.PI;
				Sack s = worldServer.SendGetSack(creatureId);
			}

			return response;
		}


        /// <summary>
        /// Executes the action selected by the clarion agent
        /// </summary>
        /// <param name="externalAction"></param>
		void processSelectedAction(CreatureActions externalAction)
		{   Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			if (worldServer != null && worldServer.IsConnected)
			{
                
                currentAction = externalAction.ToString();
                
                switch (externalAction)
                {
                    case CreatureActions.GO_BACK:
                        worldServer.SendSetAngle(creatureId, -0.5, -0.5, prad);
                        // Do nothing as the own value says
                        break;
                    case CreatureActions.ROTATE_RIGHT:
                        worldServer.SendSetAngle(creatureId, 0.3, -0.3, 0.3);
                        break;
                    case CreatureActions.ROTATE_LEFT:
                        worldServer.SendSetAngle(creatureId, -0.3, 0.3, -0.3);
                        break;
                    case CreatureActions.GO_AHEAD:
                        worldServer.SendSetAngle(creatureId, 1, 1, prad);
                        break;
                    case CreatureActions.EAT_FOOD:
                        if (thingToEat != null && thingToEat.DistanceToCreature <= 25 && (thingToEat.CategoryId == Thing.categoryPFOOD || thingToEat.CategoryId == Thing.CATEGORY_NPFOOD))
                        {
                            worldServer.SendEatIt(creatureId, thingToEat.Name);
                            thingToEat = null;
                        }
                        break;
                    case CreatureActions.GET_ITEM:
                        if (thingToGet != null && thingToGet.CategoryId == Thing.CATEGORY_JEWEL && thingToGet.DistanceToCreature <=30)
                        {
                            worldServer.SendSackIt(creatureId, thingToGet.Name);
                            //manually update the creature leaflet
                            //foreach (LeafletItem li in mycreature.getLeaflets().First().items)
                            //{
                            //    if (li.itemKey.Equals(thingToGet.Material.Color))
                            //        if (li.totalNumber > 0)
                            //        {
                            //            li.totalNumber = li.totalNumber - 1;
                            //            li.collected = li.collected + 1;
                            //            updateLeaflet(mycreature);
                            //            updateConsole();
                            //            break;
                            //        }
                            //}
                            
                            if (leaflet1[parseColor(thingToGet.Material.Color)] > 0)
                                leaflet1[parseColor(thingToGet.Material.Color)]--;

                            thingToGet = null;
                        }
                        break;
                    default:
                        break;
                }
			}
		}

        /// <summary>
        /// Setup agent infra structure (ACS, NACS, MS and MCS)
        /// </summary>
        private void SetupAgentInfraStructure()
        {
            SimplifiedQBPNetwork net = AgentInitializer.InitializeImplicitDecisionNetwork(CurrentAgent, SimplifiedQBPNetwork.Factory);

            //defining GOALS nodes
            net.Input.Add(gSearchFood, "goals");
            net.Input.Add(gSearchGems, "goals");

            //Defining visual input nodes
            net.Input.Add(inputWallAhead  );
            net.Input.Add(inputGemAhead   );
            net.Input.Add(inputFoodAhead  );
            net.Input.Add(inputGemUpRight );
            net.Input.Add(inputGemUpLeft  );
            net.Input.Add(inputGemUpFront );
            net.Input.Add(inputFoodUpRight);
            net.Input.Add(inputFoodUpLeft );
            net.Input.Add(inputFoodUpFront);

            //defining output nodes
            net.Output.Add(eacDO_NOTHING  );
            net.Output.Add(eacROTATE_RIGHT);
            net.Output.Add(eacROTATE_LEFT );
            net.Output.Add(eacGO_AHEAD    );
            net.Output.Add(eacGET_ITEM    );
            net.Output.Add(eacEAT_FOOD    );


            net.Parameters.LEARNING_RATE = 1;
            CurrentAgent.Commit(net);
                      
            CurrentAgent.ACS.Parameters.VARIABLE_BL_BETA = .5;
            CurrentAgent.ACS.Parameters.VARIABLE_RER_BETA = .5;
            CurrentAgent.ACS.Parameters.VARIABLE_IRL_BETA = 0;
            CurrentAgent.ACS.Parameters.VARIABLE_FR_BETA = 0;

            RefineableActionRule.GlobalParameters.SPECIALIZATION_THRESHOLD_1 = -.6;
            RefineableActionRule.GlobalParameters.GENERALIZATION_THRESHOLD_1 = -.1;
            RefineableActionRule.GlobalParameters.INFORMATION_GAIN_OPTION = RefineableActionRule.IGOptions.PERFECT;

            //Initialising food drive
            FoodDrive foodDrive = AgentInitializer.InitializeDrive(CurrentAgent, FoodDrive.Factory,1.0, (DeficitChangeProcessor)FoodDrive_DeficitChange);
            DriveEquation foodDriveEQ = AgentInitializer.InitializeDriveComponent(foodDrive, DriveEquation.Factory);

            foodDrive.Commit(foodDriveEQ);
            CurrentAgent.Commit(foodDrive);

            //Initialising gem drive
            AutonomyDrive gemDrive = AgentInitializer.InitializeDrive(CurrentAgent, AutonomyDrive.Factory,1.0, (DeficitChangeProcessor)GemDrive_DeficitChange);
            DriveEquation gemDriveEQ = AgentInitializer.InitializeDriveComponent(gemDrive, DriveEquation.Factory);

            gemDrive.Commit(gemDriveEQ);
            CurrentAgent.Commit(gemDrive);

            //initialising goal module and equation
            GoalSelectionModule gsm = AgentInitializer.InitializeMetaCognitiveModule(CurrentAgent, GoalSelectionModule.Factory);
            GoalSelectionEquation gse = AgentInitializer.InitializeMetaCognitiveDecisionNetwork(gsm, GoalSelectionEquation.Factory);

            gse.Input.Add(gemDrive.GetDriveStrength());
            gse.Input.Add(foodDrive.GetDriveStrength());

            GoalStructureUpdateActionChunk fu = World.NewGoalStructureUpdateActionChunk();
            GoalStructureUpdateActionChunk nu = World.NewGoalStructureUpdateActionChunk();

            nu.Add(GoalStructure.RecognizedActions.SET_RESET, gSearchGems);
            fu.Add(GoalStructure.RecognizedActions.SET_RESET, gSearchFood);
  
            gse.Output.Add(nu);
            gse.Output.Add(fu);

            gsm.SetRelevance(nu, gemDrive, 1);
            gsm.SetRelevance(fu, foodDrive, 1);

            //commits drives
            gsm.Commit(gse);
            CurrentAgent.Commit(gsm);

            //goal should be activated to full extension when it is selected
            CurrentAgent.MS.Parameters.CURRENT_GOAL_ACTIVATION_OPTION = MotivationalSubsystem.CurrentGoalActivationOptions.FULL;
        }
        
        /// <summary>
        /// Saves the current state of the creature
        /// </summary>
        /// <param name="listOfThings"></param>
        private void stateToMemory(IList<Thing> listOfThings)
        {
            Thing cthing = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_CREATURE)).First();
            Creature c = (Creature)cthing;
           
            currentFUEL = c.Fuel;
       
            currentLeaf = updateLeaflet(c);
           
            currentFoodInFront = listOfThings.Where(item => (upfront(cthing, item) && (item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD))).Any();

            currentFoodInSide = listOfThings.Where(item => ((upRight(cthing, item)|| upLeft(cthing, item)) && (item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD))).Any();

            currentGemInFront = listOfThings.Where(item => (upfront(cthing, item) && item.CategoryId == Thing.CATEGORY_JEWEL && leaflet1[parseColor(item.Material.Color)] > 0)).Any();

            currentGemInSide = listOfThings.Where(item => ((upRight(cthing, item) || upLeft(cthing, item)) && item.CategoryId == Thing.CATEGORY_JEWEL && leaflet1[parseColor(item.Material.Color)] > 0)).Any();

            currentObjTouch = listOfThings.Where(item => (item.DistanceToCreature<40 && item.CategoryId == Thing.CATEGORY_CREATURE)).Any();
        }

        /// <summary>
        /// Make the agent perception. Translate the information to feed each Input Node.
        /// </summary>
        /// <param name="sensorialInformation">The information that came from server</param>
        /// <returns>The perceived information</returns>
		private SensoryInformation prepareSensoryInformation(IList<Thing> listOfThings)
        {
            // New sensory information
            SensoryInformation si = World.NewSensoryInformation(CurrentAgent);
            si[FoodDrive.MetaInfoReservations.STIMULUS, typeof(FoodDrive).Name] = 1;
            bool aux;
            double activation;

            Thing cthing = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_CREATURE)).First();
            Creature c = (Creature)cthing;
            mycreature = c;

            // Detect if we have a wall ahead
            aux = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_BRICK && item.DistanceToCreature <= 65)).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputWallAhead, activation);

            // Detect if we have gem ahead
            aux = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_JEWEL && item.DistanceToCreature <= 40)).Any();
            if (aux)
            thingToGet = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_JEWEL && item.DistanceToCreature <= 40)).First();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputGemAhead, activation);

            // Detect if we have food ahead
            aux = listOfThings.Where(item => ((item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD) && item.DistanceToCreature <= 40)).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            if(aux)
            thingToEat = listOfThings.Where(item => ((item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD) && item.DistanceToCreature <= 40)).First();
            si.Add(inputFoodAhead, activation);

            // Detect if we have food Up front
            aux = listOfThings.Where(item => (upfront(cthing, item) && (item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD))).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputFoodUpFront, activation);

            // Detect if we have food Up right
            aux = listOfThings.Where(item => (upRight(cthing, item) && (item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD))).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputFoodUpRight, activation);

            // Detect if we have food Up left
            aux = listOfThings.Where(item => (upLeft(cthing, item) && (item.CategoryId == Thing.categoryPFOOD || item.CategoryId == Thing.CATEGORY_NPFOOD))).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputFoodUpLeft, activation);

            // Detect if we have Gem Up front
            aux = listOfThings.Where(item => (upfront(cthing, item) && item.CategoryId == Thing.CATEGORY_JEWEL && leaflet1[parseColor(item.Material.Color)] > 0)).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputGemUpFront, activation);

            // Detect if we have Gem Up right
            aux = listOfThings.Where(item => (upRight(cthing, item) && item.CategoryId == Thing.CATEGORY_JEWEL && leaflet1[parseColor(item.Material.Color)] > 0)).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputGemUpRight, activation);

            // Detect if we have Gem Up left
            aux = listOfThings.Where(item => (upLeft(cthing, item) && item.CategoryId == Thing.CATEGORY_JEWEL && leaflet1[parseColor(item.Material.Color)] > 0)).Any();
            activation = aux ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputGemUpLeft, activation);

            int n = 0;
            updateLeaflet(c);
            return si;
        }
        

        /// <summary>
        /// Runs 1 cognitive cycle. Perceive world > makes decision > send action > wait a bit > give feedback of action results.
        /// </summary>
        /// <param name="obj"></param>
        private void CognitiveCycle(object obj)
        {

			Console.WriteLine("Starting Cognitive Cycle ... press CTRL-C to finish !");
            // Cognitive Cycle starts here getting sensorial information
            while (CurrentCognitiveCycle != MaxNumberOfCognitiveCycles)
            {

                // Get current sensory information                    
                IList<Thing> currentSceneInWS3D = processSensoryInformation();

                //Saves the current state of teh creatue on the memory
                stateToMemory(currentSceneInWS3D);

                // Make the perception
                SensoryInformation si = prepareSensoryInformation(currentSceneInWS3D);
                si[AutonomyDrive.MetaInfoReservations.STIMULUS, typeof(AutonomyDrive).Name] = 1;
                si[FoodDrive.MetaInfoReservations.STIMULUS, typeof(FoodDrive).Name] = 1;

                //Perceive the sensory information
                CurrentAgent.Perceive(si);

                //Choose an action
                ExternalActionChunk chosen = CurrentAgent.GetChosenExternalAction(si);

                // Get the selected action
                String actionLabel = chosen.LabelAsIComparable.ToString();
                CreatureActions actionType = (CreatureActions)Enum.Parse(typeof(CreatureActions), actionLabel, true);

                // Call the output event handler
                processSelectedAction(actionType);

                // Increment the number of cognitive cycles
                CurrentCognitiveCycle++;
                TotalCognitiveCycle++;

                //Wait to the agent accomplish his job
                if (TimeBetweenCognitiveCycles > 0)
                {
                    Thread.Sleep(TimeBetweenCognitiveCycles);
                }
                worldServer.SendSetAngle(creatureId, 0.0, 0.0, prad);

                //update current memory and last memory state
                thelastFUEL = currentFUEL;
                thelastLeaf = currentLeaf;
                thelastFoodInFront = currentFoodInFront;
                thelastFoodInSide = currentFoodInSide;
                thelastGemInFront = currentGemInFront;
                thelastGemInSide = currentGemInSide;
                thelastObjTouch = currentObjTouch;

                currentSceneInWS3D = processSensoryInformation();
                stateToMemory(currentSceneInWS3D);

                //give feed back to network
                CurrentAgent.ReceiveFeedback(si, giveFeedBack(actionType));
                updateConsole();

                //Reset creature and world if it has died or if leaflet completed
                if (updateLeaflet(mycreature) == 0) completedLeaflets++;
                if (mycreature.Fuel <= 0 || updateLeaflet(mycreature)==0)
                {
                    worldServer.SendWorldReset();
                    worldServer.NewCreature(400, 240, 0, out creatureId, out creatureName);
                    worldServer.SendCreateLeaflet();
                    Random rand = new Random(DateTime.Now.Millisecond);
                    int j = 0;
                    int f = 1;
                    double ang = 0;
                    for (int i = 1; i < 25; i++)
                    {
                        ang = (i - 1) * 0.261;
                        if (f % 4 == 0)
                        {
                            worldServer.NewFood(0, (int)(Math.Cos(ang) * 250) + 400, (int)(Math.Sin(ang) * 250) + 300);
                        }
                        else
                        {
                            worldServer.NewJewel(j, (int)(Math.Cos(ang) * 250) + 400, (int)(Math.Sin(ang) * 250) + 300);
                            if (j > 4) j = 0;
                            else j++;
                        }
                        f++;
                    }
                    worldServer.SendStartCamera(creatureId);
                    worldServer.SendStartCreature(creatureId);
                    leaflet1 = new int[6] { 0, 0, 0, 0, 0, 0 };
                    leaflet1[rand.Next(0, 5)]=3;
                    //leaflet1[rand.Next(0, 5)]++;
                    //leaflet1[rand.Next(0, 5)]++;
                    updateLeaflet(mycreature);
                    creatureLife++;
                    CurrentCognitiveCycle = 0;
                }

                //Every 10 cycles, write learned rules to a .txt file
                if (CurrentCognitiveCycle % 10 == 0)
                {
                    if (!File.Exists("LearnedRules.txt") == true)
                        File.WriteAllText("LearnedRules.txt", "");

                    StreamWriter configWriter;
                    configWriter = new StreamWriter("LearnedRules.txt", false);
                    configWriter.Write("After " + TotalCognitiveCycle + " cogcycles within " + creatureLife + " creature lifes, the creature completed " + completedLeaflets + " leaflets");
                    configWriter.Write("\r\nIt has also learned the following rules:");
                    foreach (var i in CurrentAgent.GetInternals(Agent.InternalContainers.ACTION_RULES))
                    {
                        configWriter.Write("\r\n" + i);
                        //Console.WriteLine("\r\n" + i.ToString());
                    }
                    configWriter.Close();
                }
            }
        }
        

        /// <summary>
        /// Gives feed back to the agent based on previous state and new state
        /// </summary>
        /// <param name="actionType"></param>
        /// <returns></returns>
        private double giveFeedBack(CreatureActions actionType)
        {
            
            if (CurrentAgent.CurrentGoal == gSearchFood)
            {
                if (thelastFUEL >= 300) 
                    currentFeedBack = 0;
                else if (currentFUEL > thelastFUEL)
                    currentFeedBack = 1;
                else if (currentFoodInFront == true && thelastFoodInFront == false)
                    currentFeedBack = 0.9;
                else if (currentFoodInFront == false && thelastFoodInFront == true && actionType != CreatureActions.GO_AHEAD)
                    currentFeedBack = 0.0;
                else if (thelastFoodInFront == true && thelastObjTouch==false && actionType == CreatureActions.GO_AHEAD)
                    currentFeedBack = 1.0;
                else if (currentFoodInSide == true && thelastFoodInSide == false)
                    currentFeedBack = 0.9;
                else if (thelastFoodInFront == false && thelastFoodInSide == false && actionType == CreatureActions.ROTATE_RIGHT)
                    currentFeedBack = 0.5;
                else if (currentFoodInSide == true && thelastFoodInSide == true && actionType != CreatureActions.GO_BACK)
                    currentFeedBack = 0.5;
                else if (currentFoodInSide == false && thelastFoodInSide == true)
                    currentFeedBack = 0.0;
                else if ((currentFoodInSide == true || currentFoodInFront == true) && actionType == CreatureActions.GO_BACK)
                    currentFeedBack = 0.0;
                else
                    currentFeedBack = 0.0;
                return currentFeedBack;
            }
            else if (CurrentAgent.CurrentGoal == gSearchGems)
            {
                if (thelastFUEL < 300)
                    currentFeedBack = 0;
                else if (currentLeaf < thelastLeaf)
                    currentFeedBack = 1;
                else if (currentGemInFront == true && thelastGemInFront == false)
                    currentFeedBack = 0.9;
                else if (currentGemInFront == false && thelastGemInFront == true && actionType != CreatureActions.GO_AHEAD)
                    currentFeedBack = 0.0;
                else if (thelastGemInFront == true && thelastObjTouch == false && actionType == CreatureActions.GO_AHEAD)
                    currentFeedBack = 1.0;
                else if (currentGemInSide == true && thelastGemInSide == false)
                    currentFeedBack = 0.9;
                else if (thelastGemInFront == false && thelastGemInSide == false && actionType == CreatureActions.ROTATE_RIGHT)
                    currentFeedBack = 0.5;
                else if (currentGemInSide == true && thelastGemInSide == true && actionType != CreatureActions.GO_BACK)
                    currentFeedBack = 0.5;
                else if (currentGemInSide == false && thelastGemInSide == true)
                    currentFeedBack = 0.0;
                else if ((currentGemInSide == true || currentGemInFront == true)&& actionType == CreatureActions.GO_BACK)
                    currentFeedBack = 0.0;
                else
                    currentFeedBack = 0.0;
                return currentFeedBack;

            }
            currentFeedBack = 0.5;
            return currentFeedBack;
        }

        /// <summary>
        /// Returns true if a THING is in front of the creatue view (3 degrees margin)
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        private bool upfront(Thing creature, Thing thing)
        {
            double Cang = creature.Pitch % 360;
            double xDiff = thing.comX - creature.X1;
            double yDiff = thing.comY - creature.Y1;
            double Tang = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            if (Math.Abs(Cang - Tang) <= 5 || Math.Abs(Cang - Tang) >= 355)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if a THING is in the right side of creature view
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        private bool upRight(Thing creature, Thing thing)
        {

            double Cang = creature.Pitch % 360;
            double xDiff = thing.comX - creature.X1;
            double yDiff = thing.comY - creature.Y1;
            double Tang = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            //Console.WriteLine("CreatureXY=" + creature.X1 + "," + creature.Y1 + " ThingXY=" + thing.comX + "," + thing.comY);
            //Console.WriteLine("CreaturePitch=" + Cang + " Thingpitch(id:" + thing.CategoryId + ")=" + Tang);
            double minang = (Cang + 5);
            double maxang = (Cang + 90);
            if (minang<0) minang= 360+minang;
            if ((Tang > minang && Tang < maxang) || (Tang + 360 > minang && Tang + 360 < maxang))
            {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns true if a THING is in the left side of creature view
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        private bool upLeft(Thing creature, Thing thing)
        {
            double Cang = creature.Pitch % 360;
            double xDiff = thing.comX - creature.X1;
            double yDiff = thing.comY - creature.Y1;
            double Tang = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            double minang = (Cang - 90);
            double maxang = (Cang - 5);
            //Console.WriteLine("CreaturePitch=" + Cang + " Thingpitch(id:" + thing.CategoryId + ")=" + Tang);
            if ((Tang > minang && Tang < maxang) || (Tang - 360 > minang && Tang - 360 < maxang))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the index INT for each color STRING
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public int parseColor(string color)
        {
            switch (color)
            {
                case "Red":
                    return 0;
                case "Green":
                    return 1;
                case "Blue":
                    return 2;
                case "Yellow":
                    return 3;
                case "Magenta":
                    return 4;
                case "White":
                    return 5;
                default:
                    Console.WriteLine("ERROR: color parser doesnt knows the color: " + color);
                    return -1;
            }
        }

        /// <summary>
        /// Calculates the food Drive
        /// </summary>
        /// <param name="si"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private double FoodDrive_DeficitChange(ActivationCollection si, Drive target)
        {
            if (currentFUEL < 300)
                return 1.0;
            return 0.0;
        }

        /// <summary>
        /// Calculates the jewel drive
        /// </summary>
        /// <param name="si"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private double GemDrive_DeficitChange(ActivationCollection si, Drive target)
        {
            if (currentFUEL>=300)
                return 1.0;
            return 0.0;
        }

    }
}
