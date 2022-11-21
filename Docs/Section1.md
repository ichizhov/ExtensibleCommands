[[Next section]](Section2.md)       

[[Table of contents]](TableOfContent.md)

# 1. Introduction.

Software designers are on a continuous quest for better ways to design compact, scalable, maintainable, and highly functional software. One of the popular directions is the use of “design patterns” which represent reusable conceptual approaches to commonly encountered software design problems. These ideas have been formulated by the ["Gang of Four"](https://springframework.guru/gang-of-four-design-patterns/) and since gained a lot of following practically becoming the mainstay of modern SW engineering. The “art” of the software designer is to pick the correct pattern, and skillfully modify it as needed for a particular application to obtain maximum benefits.

The main function of a typical “machine control” software application is to orchestrate steps of an industrial or manufacturing process in a complex piece(s) of equipment. One of the main and specific challenges was to design, implement and maintain a hierarchy of operations that could be combined into progressively complex sequences, as well as called and executed individually. 

During the design and implementation of such applications relying on standard toolsets of high-level programming languages such as C++, Java, or C#, a number of issues invariably came up:
1. Structure and content of various sequences were a highly fluid area due to incomplete initial requirements, unknown aspects of non-software components' behavior and responses, additional requirements, as well as continuous improvement, to achieve more optimized or faster execution. This invariably caused a high volume of software changes in the areas of the code that arranged individual steps in high-level sequences.
2. As a result of these changes, it became challenging to maintain the quality of the code since:
   - Re-testing “happy-case” scenarios required the development of testing harnesses to “mock out” hardware interfaces, or testing required the presence of expensive hardware.
   - Testing of failure cases required even more dedicated development of test harnesses capable of injecting errors in various timing scenarios, or even more sophisticated testing using hardware.
      
In my experience, in real-life software development within a limited time and budget, areas described in (2) invariably got compromised. This resulted at best in the accumulation of technical debt and software complexity increase over time, and, at worst, the proliferation of hard-to-fix defects, project delays, and missed deadlines.

This work was motivated by the desire to find a better way to deal with the high volatility of operational sequence code, and encapsulate many aspects of this volatility behind a dedicated infrastructural implementation. The conceptual approach to software design employing volatility-based decomposition has been recently popularized by Juval Löwy in the book ["Righting Sofware"](https://www.idesign.net/Books/Righting-Software). One of the main topics discussed at length in this book is a structured method to identify areas of volatility in the system and design software by encapsulating these areas of volatility rather than the domain or functional areas. In the context of volatility-based decomposition, one can view this project as a framework to encapsulate the high volatility of operational sequence code.

This document describes the conceptual approach as well as the library implementation in C# (a mirror implementation of the same library is available in Java), henceforth collectively referred to as “Extensible Commands”.

[[Next section]](Section2.md)       

[[Table of contents]](TableOfContent.md)

