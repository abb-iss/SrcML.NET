using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public abstract class AbstractProgramElement {
        /// <summary>Returns the parent of this element.</summary>
        protected abstract AbstractProgramElement GetParent();
        /// <summary>Returns the children of this element.</summary>
        protected abstract IEnumerable<AbstractProgramElement> GetChildren();

        /// <summary>
        /// Gets all of the parents of this element
        /// </summary>
        /// <returns>The parents of this element</returns>
        public IEnumerable<AbstractProgramElement> GetAncestors() {
            return GetAncestorsAndStartingPoint(this.GetParent());
        }

        /// <summary>
        /// Gets all of the parents of type <typeparamref name="T"/> of this element.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent elements by</typeparam>
        /// <returns>The parents of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestors<T>() where T : AbstractProgramElement {
            return GetAncestorsAndStartingPoint(this.GetParent()).OfType<T>();
        }

        /// <summary>
        /// Gets all of parents of this element as well as this element.
        /// </summary>
        /// <returns>This element followed by its parents</returns>
        public IEnumerable<AbstractProgramElement> GetAncestorsAndSelf() {
            return GetAncestorsAndStartingPoint(this);
        }

        /// <summary>
        /// Gets all of the parents of this element as well as the element itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent elements by</typeparam>
        /// <returns>This element followed by its parent elements where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestorsAndSelf<T>() where T : AbstractProgramElement {
            return GetAncestorsAndStartingPoint(this).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendant elements of this statement. This is every element that is rooted at this element.
        /// </summary>
        /// <returns>The descendants of this statement</returns>
        public IEnumerable<AbstractProgramElement> GetDescendants() {
            return GetDescendants(this, false);
        }

        /// <summary>
        /// Gets all of the descendant elements of this element where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant elements by</typeparam>
        /// <returns>The descendants of type <typeparamref name="T"/> of this element</returns>
        public IEnumerable<T> GetDescendants<T>() where T : AbstractProgramElement {
            return GetDescendants(this, false).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendants of this element as well as the element itself.
        /// </summary>
        /// <returns>This element, followed by all of its descendants</returns>
        public IEnumerable<AbstractProgramElement> GetDescendantsAndSelf() {
            return GetDescendants(this, true);
        }

        /// <summary>
        /// Gets all of the descendants of this element as well as the element itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant elements by</typeparam>
        /// <returns>This element and its descendants where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetDescendantsAndSelf<T>() where T : AbstractProgramElement {
            return GetDescendants(this, true).OfType<T>();
        }

        /// <summary>
        /// Gets an element and all of its ancestors
        /// </summary>
        /// <param name="startingPoint">The first element to return</param>
        /// <returns>The <paramref name="startingPoint"/> and all of its ancestors</returns>
        protected static IEnumerable<AbstractProgramElement> GetAncestorsAndStartingPoint(AbstractProgramElement startingPoint) {
            var current = startingPoint;
            while(null != current) {
                yield return current;
                current = current.GetParent();
            }
        }

        /// <summary>
        /// Gets the <paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and all of the descendants of the <paramref name="startingPoint"/>.
        /// </summary>
        /// <param name="startingPoint">The starting point</param>
        /// <param name="returnStartingPoint">If true, return the starting point first. Otherwise, just return  the descendants.</param>
        /// <returns><paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and its descendants</returns>
        protected static IEnumerable<AbstractProgramElement> GetDescendants(AbstractProgramElement startingPoint, bool returnStartingPoint) {
            if(returnStartingPoint) {
                yield return startingPoint;
            }

            foreach(var element in startingPoint.GetChildren()) {
                foreach(var descendant in GetDescendants(element, true)) {
                    yield return descendant;
                }
            }
        }

        
    }
}
