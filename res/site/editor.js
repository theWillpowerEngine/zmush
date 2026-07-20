const editor = {
    formatCode(s) {
        if (s.trim()[0] != "{") {
            s = "{" + s + "}"
        }
        var code = readCode(s)

        const format = (code, depth) => {
            let result = ""
            for (const item of code) {
                if (typeof item === "string") {

                    if (item.indexOf(" ") > -1) {
                        if (item.indexOf("'") === -1)
                            item = "'" + item + "'"
                        else if (item.indexOf("\"") === -1)
                            item = "\"" + item + "\""
                        else
                            item = "`" + item + "`"
                    }

                    result += " " + item
                } else if (Array.isArray(item)) {
                    result += "\n" + "  ".repeat(depth) + "{"
                    result += format(item, depth + 1).trim()
                    result += "}"
                }
            }
            return result
        }

        return format(code, 0).trim()
    },

    deformatCode(s) {
        if (s.trim()[0] != "{") {
            s = "{" + s + "}"
        }
        var code = readCode(s)
        const deformat = (code) => {
            let result = ""
            for (const item of code) {
                if (typeof item === "string") {
                    result += " " + item
                } else if (Array.isArray(item)) {
                    result += " {"
                    result += deformat(item).trim()
                    result += "}"
                }
            }
            return result
        }

        return deformat(code).trim()
    }
}


function escapeChar(ch) {
    if (ch === undefined) return ""
    switch (ch) {
        case "s": return "&nbsp"
        case "t": return "&emsp"
        case "n": return "<br />"
        default: return ch
    }
}

function scanTo(s, state, terminator, opener) {
    let depth = 0
    let work = ""

    state.i += 1 // skip the opener

    for (; state.i < s.length; state.i++) {
        const c = s[state.i]

        if (c === terminator && depth === 0) return work

        if (opener != null && c === opener) depth += 1
        else if (c === terminator && depth > 0) depth -= 1

        work += c
    }

    return work // ran off the end unterminated — best effort
}

function readCode(code) {
    const result = []
    let work = ""
    let stringDelim = null // null == "not currently inside a string"

    const addWork = () => {
        if (work.trim() !== "") {
            result.push(work)
            work = ""
        }
    }

    const state = { i: 0 }
    for (state.i = 0; state.i < code.length; state.i++) {
        const c = code[state.i]
        const lookAhead = state.i + 1 < code.length ? code[state.i + 1] : undefined

        // --- inside a string literal ---
        if (stringDelim !== null) {
            if (c === "%") {
                state.i += 1
                work += escapeChar(code[state.i])
            } else if (c === stringDelim) {
                work = stringDelim + work + stringDelim
                addWork()
                stringDelim = null
            } else {
                work += c
            }
            continue
        }

        // --- comments ---
        if (c === "") {
            addWork()

            if (lookAhead === "{") {
                // { commented-out block }
                state.i += 1
                scanTo(code, state, "}", "{") // discard
            } else {
                // regular comment: runs to end-of-line or a closing ''
                let j = state.i + 1
                while (j < code.length && code[j] !== "" && code[j] !== "\r" && code[j] !== "\n") {
                    j += 1
                }
                state.i = j
            }
            continue
        }

        switch (c) {
            case " ":
            case "\t":
            case "\r":
            case "\n":
                addWork()
                break

            case "{": {
                addWork()
                const inner = scanTo(code, state, "}", "{")
                result.push(readCode(inner)) // nested list
                break
            }

            case "}":
                break

            case "[": {
                addWork()
                const autoletCode = scanTo(code, state, "]")
                result.push("let", ...readCode(autoletCode))
                break
            }

            case '"':
            case "`":
            case "'":
                addWork()
                stringDelim = c
                break

            default:
                work += c
                break
        }
    }

    if (stringDelim !== null) {
        if (work !== "") result.push(stringDelim === "`" ? ["str", work] : work)
        work = ""
    }

    addWork()
    return result
}