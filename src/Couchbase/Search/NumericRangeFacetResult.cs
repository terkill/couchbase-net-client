using System.Collections.Generic;
using Newtonsoft.Json;

namespace Couchbase.Search
{
    /// <summary>
    /// The result for a <see cref="NumericRangeFacet"/>.
    /// </summary>
    public class NumericRangeFacetResult : DefaultFacetResult
    {
        public NumericRangeFacetResult()
        {
            NumericRanges = new List<NumericRange>();
        }

        /// <summary>
        /// Gets or sets the numeric ranges.
        /// </summary>
        /// <value>
        /// The numeric ranges.
        /// </value>
        [JsonProperty("numericRanges")]
        public IReadOnlyCollection<NumericRange> NumericRanges { get; set; }

        /// <summary>
        /// Gets the type of the facet result.
        /// </summary>
        /// <value>
        /// The type of the facet result.
        /// </value>
        [JsonProperty("facetResultType")]
        public override FacetResultType FacetResultType => FacetResultType.NumericRange;
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
