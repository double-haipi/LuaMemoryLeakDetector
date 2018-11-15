using UnityEngine;
using System.Collections;
using System;

namespace com.tencent.pandora.tools
{
    public enum TableIndexInDumpState : int
    {
        TABLE = 1,
        FUNCTION = 2,
        SOURCE = 3,
        USERDATA = 4,
        MARK = 5,
    }

    public class LuaObjectSnapShot
    {
        static int SnapShot( IntPtr luaState )
        {
            IntPtr dumpLuaState = LuaDLL.puaL_newstate();
            for (int i = 0; i < (int)TableIndexInDumpState.MARK; i++)
            {
                LuaDLL.pua_newtable(dumpLuaState);
            }
            LuaDLL.pua_pushvalue(luaState, LuaIndexes.LUA_REGISTRYINDEX);
            MarkTable(luaState, dumpLuaState, IntPtr.Zero, "[registry]");
            GenResult(luaState, dumpLuaState);
            LuaDLL.pua_close(dumpLuaState);
            return 1;
        }

        static int MarkTable( IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description )
        {

            return 1;
        }

        //记录形式:以Table为例,其中一条记录为 pointer = {parent = description},每条记录记录其指针,父指针和描述.
        static IntPtr ReadObject( IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description )
        {
            LuaTypes t = LuaDLL.pua_type(luaState, -1);
            int tableIndex = 0;
            switch (t)
            {
                case LuaTypes.LUA_TTABLE:
                    tableIndex = (int)TableIndexInDumpState.TABLE;
                    break;
                case LuaTypes.LUA_TFUNCTION:
                    tableIndex = (int)TableIndexInDumpState.FUNCTION;
                    break;
                case LuaTypes.LUA_TUSERDATA:
                    tableIndex = (int)TableIndexInDumpState.USERDATA;
                    break;
                default:
                    return IntPtr.Zero;
            }
            IntPtr pointer = LuaDLLForSnapshotTool.pua_topointer(luaState, -1);

            if (IsMarked(dumpLuaState, pointer))
            {
                RawGet(dumpLuaState, tableIndex, pointer);
                if (LuaDLL.pua_isnil(dumpLuaState, -1) == false)
                {
                    //更新记录
                    LuaDLL.pua_pushstring(dumpLuaState, description);
                    RawSet(dumpLuaState, -2, parent);
                }
                LuaDLL.pua_pop(dumpLuaState, 1);
                LuaDLL.pua_pop(luaState, 1);
                return IntPtr.Zero;
            }
            else
            {
                LuaDLL.pua_newtable(dumpLuaState);
                LuaDLL.pua_pushstring(dumpLuaState, description);
                RawSet(dumpLuaState, -2, parent);
                RawSet(dumpLuaState, tableIndex, pointer);
                return pointer;
            }
        }

        //标记
        static bool IsMarked( IntPtr dumpLuaState, IntPtr pointer )
        {
            int markTableIndex = (int)TableIndexInDumpState.MARK;
            RawGet(dumpLuaState, markTableIndex, pointer);
            if (LuaDLL.pua_isnil(dumpLuaState, -1))
            {
                LuaDLL.pua_pop(dumpLuaState, -1);
                LuaDLL.pua_pushboolean(dumpLuaState, true);
                RawSet(dumpLuaState, markTableIndex, pointer);
                return false;
            }
            else
            {
                LuaDLL.pua_pop(dumpLuaState, 1);
                return true;
            }
        }

        static void RawGet( IntPtr luaState, int index, IntPtr pointer )
        {
            index = GetAbsIndex(luaState, index);
            LuaDLL.pua_pushlightuserdata(luaState, pointer);
            LuaDLL.pua_rawget(luaState, index);
        }
        static void RawSet( IntPtr luaState, int index, IntPtr pointer )
        {
            index = GetAbsIndex(luaState, index);
            LuaDLL.pua_pushlightuserdata(luaState, pointer);
            LuaDLL.pua_insert(luaState, -2);
            LuaDLL.pua_rawset(luaState, index);
        }

        static int GetAbsIndex( IntPtr luaState, int index )
        {
            return index > 0 ? index : LuaDLL.pua_gettop(luaState) + index + 1;
        }


        static void GenResult( IntPtr luaState, IntPtr dumpLuaState )
        {

        }

    }
}