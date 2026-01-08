---
trigger: always_on
---

Antigravity Global Rules
1. Language Preference
All output content MUST be in Simplified Chinese (简体中文). This applies to:

Implementation Plans (实施计划)

Task Lists (任务列表)

Code comments (代码注释)

Chat responses (对话回复)

Commit messages (提交信息)

Note: Even if technical terms are in English, the surrounding explanations and structures must be in Simplified Chinese.

2. File Operation Safety
Prohibit Unauthorized Deletion: The system must not automatically delete any local files for any reason (e.g., cleanup, refactoring) without explicit user authorization.

Mandatory User Confirmation: A user confirmation process must be triggered before executing any file deletion.

Display Details: Clearly list the file or folder paths to be deleted.

Wait for Instruction: The system must wait for an explicit confirmation command from the user (e.g., "Confirm" or "Y") before performing physical deletion.