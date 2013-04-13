//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using Peach.Core;
//using Peach.Core.Dom;
//using NLog;

//namespace Godel.Core
//{
//    [Mutator("Changes which state we perform")]
//    public class GodelStateMutator : Mutator
//    {
//        static List<State> seenStates = new List<State>();

//        uint mutationCount = 0;

//        public GodelStateMutator(State obj)
//        {
//        }

//        /// <summary>
//        /// Check to see if DataElement is supported by this 
//        /// mutator.
//        /// </summary>
//        /// <param name="obj">DataElement to check</param>
//        /// <returns>True if object is supported, else False</returns>
//        public static new bool supportedDataElement(DataElement obj)
//        {
//            return false;
//        }

//        /// <summary>
//        /// Check to see if State is supported by this 
//        /// mutator.
//        /// </summary>
//        /// <param name="obj">State to check</param>
//        /// <returns>True if object is supported, else False</returns>
//        public static new bool supportedState(State obj)
//        {
//            if(!seenStates.Contains(obj))
//                seenStates.Add(obj);

//            return true;
//        }

//        public override int count
//        {
//            get
//            {
//                return seenStates.Count;
//            }
//        }

//        public override uint mutation
//        {
//            get
//            {
//                return mutationCount;
//            }
//            set
//            {
//                mutationCount = value;
//            }
//        }

//        /// <summary>
//        /// Allow changing which state we change to.
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <returns></returns>
//        public override State changeState(State obj)
//        {
//            return obj;
//        }

//        public override void sequentialMutation(DataElement obj)
//        {
//            throw new NotImplementedException();
//        }

//        public override void randomMutation(DataElement obj)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

//// end
