using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

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
        //#define LUA_IDSIZE	256  lua中的设置
        [StructLayout(LayoutKind.Sequential)]
        public struct LuaDebug
        {
            public int eventId;
            public string name;
            public string nameWhat;
            public string what;
            public string source;

            public int currentLine;
            public int upvalueNumbers;
            public int lineDefined;
            public int lastlineDefined;
            //数组
            //short_src
            int activeFunctionId;
        }

        private static int LUA_MINSTACK = 20;

        static int SnapShot(IntPtr luaState)
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

        static void MarkTable(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description)
        {
            IntPtr tablePointer = ReadObject(luaState, dumpLuaState, parent, description);
            if (tablePointer == IntPtr.Zero)
            {
                //感觉这里会导致栈不平衡，没有弹出不做标记的元素，后面要测试下栈顶位置。
                return;
            }

            bool weakKey = false;
            bool weakValue = false;

            //读取元表
            if (LuaDLL.pua_getmetatable(luaState, -1) != 0)
            {
                LuaDLL.pua_pushstring(luaState, "__mode");
                LuaDLL.pua_rawget(luaState, -2);
                if (LuaDLL.pua_isstring(luaState, -1))
                {
                    string mode = LuaDLL.pua_tostring(luaState, -1);
                    if (mode.Contains("k"))
                    {
                        weakKey = true;
                    }
                    if (mode.Contains("v"))
                    {
                        weakValue = true;
                    }
                }
                LuaDLL.pua_pop(luaState, 1);
                //扩展栈
                if (LuaDLL.pua_checkstack(luaState, LUA_MINSTACK))
                {
                    MarkTable(luaState, dumpLuaState, tablePointer, "[metatable]");
                }
                else
                {
                    Debug.LogWarning(string.Format("不能保证｛0｝luaState 栈上还有空余的{1}个槽", luaState, LUA_MINSTACK));
                }
            }

            //遍历table
            LuaDLL.pua_pushnil(luaState);
            while (LuaDLL.pua_next(luaState, -2) != 0)
            {
                if (weakValue)
                {
                    //弱引用的对象不用记录，这里的引用不会造成其释放不了
                    LuaDLL.pua_pop(luaState, 1);
                }
                else
                {
                    MarkObject(luaState, dumpLuaState, tablePointer, GetKeyDescription(luaState, -2));
                }

                if (weakKey == false)
                {
                    LuaDLL.pua_pushvalue(luaState, -1);
                    MarkObject(luaState, dumpLuaState, tablePointer, "[key]");
                }

            }

            LuaDLL.pua_pop(luaState, 1);
        }

        //读取时进行记录
        //记录形式:以Table为例,其中一条记录为 pointer = {parent = description},每条记录记录其指针,父指针和描述.
        static IntPtr ReadObject(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description)
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
            IntPtr pointer = LuaDLL.pua_topointer(luaState, -1);

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
        static bool IsMarked(IntPtr dumpLuaState, IntPtr pointer)
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

        static void RawGet(IntPtr luaState, int index, IntPtr pointer)
        {
            index = GetAbsIndex(luaState, index);
            LuaDLL.pua_pushlightuserdata(luaState, pointer);
            LuaDLL.pua_rawget(luaState, index);
        }
        static void RawSet(IntPtr luaState, int index, IntPtr pointer)
        {
            index = GetAbsIndex(luaState, index);
            LuaDLL.pua_pushlightuserdata(luaState, pointer);
            LuaDLL.pua_insert(luaState, -2);
            LuaDLL.pua_rawset(luaState, index);
        }

        static int GetAbsIndex(IntPtr luaState, int index)
        {
            return index > 0 ? index : LuaDLL.pua_gettop(luaState) + index + 1;
        }


        static void GenResult(IntPtr luaState, IntPtr dumpLuaState)
        {

        }

        static string GetKeyDescription(IntPtr luaState, int index)
        {
            LuaTypes keyType = LuaDLL.pua_type(luaState, index);
            string keyDescription;
            switch (keyType)
            {
                case LuaTypes.LUA_TNIL:
                    keyDescription = "[nil]";
                    break;
                case LuaTypes.LUA_TBOOLEAN:
                    keyDescription = string.Format("[{0}]", LuaDLL.pua_toboolean(luaState, index));
                    break;
                case LuaTypes.LUA_TNUMBER:
                    keyDescription = string.Format("[{0}]", LuaDLL.pua_tonumber(luaState, index));
                    break;
                case LuaTypes.LUA_TSTRING:
                    keyDescription = LuaDLL.pua_tostring(luaState, index);
                    break;
                default:
                    keyDescription = string.Format("[{0}:{1}]", LuaDLL.pua_typenamestr(luaState, keyType), LuaDLL.pua_topointer(luaState, index));
                    break;
            }

            return keyDescription;
        }

        static void MarkObject(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description)
        {
            //扩展栈  因为要在栈上做递归，需要很多空间
            if (LuaDLL.pua_checkstack(luaState, LUA_MINSTACK) == false)
            {
                Debug.LogWarning(string.Format("不能保证｛0｝luaState 栈上还有空余的{1}个槽，有可能会出现栈OverFlow", luaState, LUA_MINSTACK));
            }

            LuaTypes objType = LuaDLL.pua_type(luaState, -1);
            switch (objType)
            {
                case LuaTypes.LUA_TTABLE:
                    MarkTable(luaState, dumpLuaState, parent, description);
                    break;
                case LuaTypes.LUA_TFUNCTION:
                    MarkFunction(luaState, dumpLuaState, parent, description);
                    break;
                case LuaTypes.LUA_TUSERDATA:
                    MarkUserdata(luaState, dumpLuaState, parent, description);
                    break;
                default:
                    LuaDLL.pua_pop(luaState, 1);
                    break;
            }
        }

        //标记其environment表和upvalues
        static void MarkFunction(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description)
        {
            IntPtr functionPointer = ReadObject(luaState, dumpLuaState, parent, description);
            if (functionPointer == IntPtr.Zero)
            {
                return;
            }
            MarkFunctionEnvironment(luaState, dumpLuaState, functionPointer);
            int i;
            for (i = 1; i < int.MaxValue; i++)
            {
                IntPtr upvalueNamePointer = LuaDLL.pua_getupvalue(luaState, -1, i);
                if (upvalueNamePointer == IntPtr.Zero)
                {
                    break;
                }
                //c 函数的upvalue name 为""
                string name = Marshal.PtrToStringAnsi(upvalueNamePointer); ;

                MarkObject(luaState, dumpLuaState, functionPointer, name != "" ? name : "[upvalue]");
            }

            //c function 有没有必要记录？有upvalue 的c function 不记录？
            if (LuaDLL.pua_iscfunction(luaState, -1))
            {
                if (i == 1)
                {
                    LuaDLL.pua_pushnil(dumpLuaState);
                    RawSet(dumpLuaState, (int)TableIndexInDumpState.FUNCTION, functionPointer);
                }
                LuaDLL.pua_pop(luaState, 1);
            }
            else
            {

            }


        }

        static void MarkFunctionEnvironment(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent)
        {
            LuaDLL.pua_getfenv(luaState, -1);
            if (LuaDLL.pua_istable(luaState, -1))
            {
                MarkObject(luaState, dumpLuaState, parent, "[environment]");
            }
            else
            {
                LuaDLL.pua_pop(luaState, 1);
            }
        }

        static void MarkUserdata(IntPtr luaState, IntPtr dumpLuaState, IntPtr parent, string description)
        {

        }

    }
}