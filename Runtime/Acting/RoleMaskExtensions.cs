namespace Readymade.Machinery.Acting
{
    public static class RoleMaskExtensions
    {
        /// <summary>
        /// Checks if the role-ID (integer value from 0 to 31) matches a bit in the role mask.
        /// </summary>
        /// <param name="mask">The mask to check.</param>
        /// <param name="roleID">The role ID to check.</param>
        /// <returns>1 if a bit matches, -1 if it doesn't, 0 if the mask is empty.</returns>
        public static int MatchRoleID(this RoleMask mask, int roleID)
        {
            // no role requirement
            if (mask == RoleMask.None)
            {
                return 0;
            }

            // match role-ID against bitmask
            bool isMaskMatch = ((int)mask & (1 << roleID)) != 0;
            return isMaskMatch ? 1 : -1;
        }
    }
}