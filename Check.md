1. SYSTEM DESIGN


Project: Online Tech Store Web Application 				       Date: December 9, 2025
________________________________________
1. Introduction to System Design
The System Design phase is the technical modeling process where the User (UR), System (SR), and Functional (FR) requirements, defined during Requirements Engineering, are transformed into a concrete software architecture. The primary objective of this report is to define how the system will perform its functions via technical diagrams and architectural decisions, rather than just describing what it does.
System modeling has been conducted through four main perspectives, in accordance with the project scope:
•	Context Perspective: Defines the system boundaries and its interactions with external entities.
•	Interaction Perspective: Models the functional flows between users and system components (Use Case & Sequence Diagrams).
•	Structural Perspective: Illustrates the static structure and data organization of the system (Class Diagram).
•	Behavioral Perspective: Models the dynamic behaviors and state transitions of the system (Activity & State Diagrams).

1.1 Context Perspective
The Context Model defines the System Boundary of the Online Tech Store, identifying the external entities that interact with the system and the information flows that cross this boundary. In this perspective, the internal architecture (Frontend, Backend, Database) is abstracted as a single "Black Box" system.
1.1.1. Context Diagram
The following diagram illustrates the operational context, showing only the actors and external services interacting with the system boundary.
 
Figure 1.1 — System Context Diagram
						 
1.1.2. Context Explanation
System Boundary The Online Tech Store Web Platform includes all internal subsystems such as the frontend SPA, backend API, and database. From the context view, these internal components are abstracted, and only the external interactions are highlighted.
Actors (External Entities)
•	Customer: Browsing/filtering products , comparing items , executing purchases (One-Click Buy).
•	Admin: Assigning system roles and managing system-wide themes.
•	Product Manager: Adding new products, managing stock/pricing , and approving/rejecting user reviews.
•	Company Owner: Requesting dashboard analytics and viewing financial summaries (read-only).
Passive External Entities (Services):
•	Mock Payment Service: An external system used solely for authorizing transactions during the "One-Click Buy" process.
o	Note: As per project constraints, payment is simulated via this mock service, and no real banking API is integrated. Similarly, notification services (Email/SMS) are excluded from the external context as alerts are handled internally.
1.1.3 Information Flow Overview
Each actor exchanges specific data with the system:
•	Customer → System: Filter criteria, Product selections, Cart operations, Purchase requests.
•	System → Customer: Filtered product results, Comparison tables, Order confirmation.
•	Admin → System: Role assignments, Theme updates.
•	Product Manager → System: Product creation & edits, Stock updates, Review moderation decisions.
•	Company Owner → System: Dashboard analytics requests.
•	System → Payment Service: PaymentRequest(oneClick=true).
•	Payment Service → System: AuthorizationResult(Success/Fail).
1.1.4 Notes on Internal Data Stores
While the system internally manages several persistent data stores—such as ProductDB and TransactionDB —these elements remain abstracted in the context model to satisfy UML context-diagram conventions.
________________________________________
1.2 Interaction Perspective (Use Case & Sequence Diagrams)
1.2.1 Use Case Diagram
                                                   
Figure 1.2— Usa Case Diagram

The functional requirements of the system are modeled through three key actors and their interactions with the system, as shown in the provided Use Case diagram (Figure 1.2).
Primary Actors:
•	Customer: The core user who performs product discovery, shopping, and interaction tasks.
•	Admin: The privileged user responsible for system configuration and role management.
•	Product Manager: The operational user responsible for inventory and review moderation.
Use Case Descriptions & Relationships:
•	Product Discovery & Filtering:
o	Search & List Products: The base use case for browsing the catalog.
o	Filter Products (Include/Exclude): This use case extends "Search & List Products" with an <<extends>> relationship. It represents the optional behavior where the user applies advanced inclusion or exclusion logic to the search results.
o	Compare Products: Allows users to select items for technical comparison.
•	Shopping & Transaction:
o	Manage Cart (Add/Remove): The standard process of handling cart items.
o	Checkout: The standard payment and order finalization flow.
o	One-Click Buy: This use case extends "Checkout" with an <<extends>> relationship. It represents the accelerated purchasing flow that bypasses standard entry forms.
•	User Interactions:
o	Add to Favorites: Users can mark items for later.
o	Write Review: Users can submit feedback for products.
o	Request Return: Users can initiate a return process for past orders.
o	Login: The authentication entry point for all actors.
•	Administrative Functions:
o	Assign Roles (Admin): Managing user permissions.
o	Change Theme (Admin): Toggling system visual settings.
o	Approve Reviews (Product Manager): Moderating user-submitted content.
o	Manage Products (Add/Edit/Stock) (Product Manager): Handling inventory updates.

1.2.2 Sequence Diagrams
The dynamic interactions between system objects for the most critical scenarios are modeled below. These diagrams illustrate the step-by-step message flow between Actors, UI, API Controllers, Services, and the Database.

Sequence 1 — Dynamic Product Comparison
This scenario demonstrates how a customer compares multiple products side-by-side based on their technical attributes.
 
Figure 1.3 — Sequence Diagram: Dynamic Product Comparison





Workflow Summary:
•	The user opens the Product Detail page.
•	ProductController fetches product specifications from the database.
•	The user adds the product to a comparison list.
•	The user then selects additional products within the same category.
•	Upon clicking “Start Comparison”, the CompareService aggregates all selected product specifications.
•	A comparison matrix is generated and displayed in the UI.
This workflow models an asynchronous and attribute-aligned comparison process, enabling dynamic filtering and matrix generation.

Sequence 2 — Advanced Filtering with Exclude Mode
This scenario models a sophisticated filtering mechanism with include/exclude logic.

 
Figure 1.4 — Sequence Diagram: Advanced Filtering with Exclude Mode

Workflow Summary:
•	The user navigates to the GPU category page.
•	Initial product results are retrieved based on the category filter.
•	The user searches for filter attributes (e.g., “Display”).
•	The user toggles the filter mode from INCLUDE → EXCLUDE.
•	The user excludes products with specific DisplayPort values (1 and 2).
•	FilterService builds a SQL NOT EXISTS query to exclude matching items.
•	Additional filters (e.g., Brand = AMD) refine the results further.
This diagram represents an intelligent filter engine supporting compound queries and exclusion logic.

Sequence 3 — One-Click Purchase (Accelerated Checkout)
This scenario models the complete one-click ordering workflow.


 
Figure 1.5 — Sequence Diagram: One-Click Purchase Workflow

Workflow Summary:
•	The user clicks “One-Click Buy” on the Cart page.
•	An order creation request is initiated with no additional user input.
•	OrderService communicates with MockPaymentService to simulate authorization.
•	Success Path: Payment succeeds → Stock is reserved → Transaction is logged → User is redirected to the Order Confirmation page.
•	Failure Path: Payment fails → System returns an error → User is redirected to the standard checkout.
This illustrates synchronous transaction handling, stock reservation, and dual success/failure flows.

Sequence 4 — Admin Role Assignment & Theme Change
This scenario demonstrates administrative control processes.
 
Figure 1.6 — Sequence Diagram: Admin Role Assignment & Theme Change
Workflow Summary:
•	The Admin logs into the system and obtains a JWT token.
•	The Admin assigns a "Product Manager" role to a selected user.
•	The UserService updates the database entry.
•	For theme configuration:
o	Admin selects “Dark Mode”, triggering a settings update in the SettingDB.
o	The UI instantly adapts to the new theme, showcasing dynamic configuration updates.

Sequence 5 — Adding a New Product with Dynamic Specifications
This scenario models the process of creating a new product with category-specific attributes.

 
Figure 1.7 — Sequence Diagram: Adding a New Product with Dynamic Specifications




Workflow Summary:
•	The Product Manager accesses the "Add New Product" screen.
•	The system retrieves available categories.
•	The user selects a category such as "Graphics Card".
•	The system retrieves dynamic fields associated with that category (e.g., VRAM, Fan Count).
•	The Product Manager fills the form, and the system saves the new product.
•	The product becomes visible to customers immediately.
This demonstrates metadata-driven form generation via category dynamic fields.

Sequence 6 — Stock Management and Product Updates
This scenario focuses on low-stock detection and restocking.
 
Figure 1.8 — Sequence Diagram: Stock Management and Product Updates

Workflow Summary:
•	The Product Manager opens the "Stock Tracking" page.
•	The system lists products where Stock <= CriticalLevel.
•	The user selects a product and updates its stock and price.
•	The system executes an UPDATE query and confirms the change.
This represents real-time inventory control and administrative data maintenance.

Sequence 7 — Comment Moderation Workflow
This scenario models product review moderation.


 
Figure 1.9 — Sequence Diagram: Comment Moderation Workflow




Workflow Summary:
•	Product Manager views all pending comments.
•	CommentService retrieves reviews waiting for approval.
•	The user selects a comment and approves it.
•	The system updates the comment status to “Approved”.
•	The comment becomes visible on the public product page.
This ensures controlled content publishing and review flow.

Sequence 8 — Sales Analytics for Company Owner
This scenario demonstrates dashboard analytics and reporting.


 
Figure 1.10 — Sequence Diagram: Sales Analytics for Company Owner



Workflow Summary:
•	The Company Owner logs into the system and opens the analytics dashboard.
•	The system fetches KPI summaries (revenue, best sellers, return rate) from the transactional database.
•	The user changes the date filter (e.g., Last 30 Days).
•	The system recalculates metrics and refreshes charts without modifying data.
This models read-only analytical queries for business insights.

________________________________________

1.3 Structural Perspective (Class Diagram)
1.3.1 Class Diagram Analysis

 
Figure 1.11 — UML Class Diagram

The system's database schema and object-oriented structure are shown in the provided Class Diagram. The entities, attributes, and associations within the system are detailed below.
Main Classes & Attributes:
•	User: The central entity containing Id, FirstName, LastName, Email, Address, and CreatedAt.
o	Methods: Register(), Login(), UpdateProfile().
•	Product: Represents the inventory items with attributes like Brand, Price, Stock, and IsActive.
o	Methods: UpdateStock().
•	Order & Transaction:
o	Order: Tracks purchase events (OrderDate, TotalAmount, Status, ShippingAddress).
o	Transaction: Logs financial history (TransactionType, Amount, TransactionDate).
•	Sub-Entities:
o	Category: Supports recursive hierarchy via ParentCategoryID.
o	ProductSpecification: Stores technical specs (e.g., RAM, CPU type).
o	ProductImage: Manages multiple images per product.
1.3.2 Class Relationships
The diagram defines the following specific associations and multiplicities:
1.	User Interactions:
o	User (1) — (*) Role: Association "Assigned to".
o	User (1) — (*) Order: Association "Places".
o	User (1) — (*) Transaction: Association "Initiates".
o	User (1) — (*) ProductReview: Association "Writes".
o	User (1) — (*) Favorite: Association "Has".
o	User (1) — (*) CartItem: Association "Adds".
2.	Product Relationships:
o	Category (1) — (*) Product: Association "Contains".
o	Product (1) — (*) ProductImage: Association "Has Images".
o	Product (1) — (*) ProductSpecification: Association "Has Specs".
o	Product (1) — (*) OrderItem: Association "Included in".
3.	Order Integrity:
o	Order (1) — (1) Transaction: Association "Generates".
o	Order (1) ♦— (*) OrderItem: A Composition relationship ("Composed of"), indicating that order items cannot exist without the parent order.
________________________________________
1.4 Behavioral Models (Activity & State Machine Diagrams)
The system's dynamic behavior is modeled from two different perspectives: Activity Diagrams, which show workflows, and State Diagrams, which show object lifecycles.


1.4.1 Activity Diagram - Advanced Filtering (Include/Exclude)

                                             
Figure 1.12 — Activity Diagram: Advanced Filtering (Include/Exclude)

This diagram illustrates the flow of the "Reverse Filtering" logic:
1.	Start: User opens the Category Page and the initial list loads.
2.	Interaction: User enters text into the Filter Search Box and selects an attribute.
3.	Decision: User toggles the Filter Mode (Include / Exclude).

4.	System Action:
o	Frontend builds the filtering payload.
o	Sends an asynchronous request to the Product API.
o	Backend validates parameters and applies Include/Exclude rules.
o	Database runs the filtered query.
5.	End: The Product Grid renders the updated list, and the Accordion Panel is updated.

1.4.2. Activity Diagram - Product Comparison


                                                            
Figure 1.13 — Activity Diagram: Product Comparison Process

This diagram details the flow of the technical comparison module:
1.	Start: User views a Product Detail Page and clicks "Compare".
2.	Interaction: The Compare Drawer opens, and the user selects additional products.
3.	System Action:
o	A comparison request is sent to the API.
o	Backend fetches product specifications.
o	Normalization: Attributes are normalized for accurate comparison.
o	Matrix Generation: A comparison matrix is generated.
4.	End: The Comparison Page is rendered. If the user modifies the list, the loop restarts to update the comparison.

1.4.3. Activity Diagram - One-Click Buy

                 
Figure 1.14 — Activity Diagram: One-Click Buy
This diagram models the accelerated checkout process with security checks:
1.	Start: User opens the Cart Page.
2.	Security Check: System checks authentication and role permissions.
o	If Unauthorized: Shows error "This role cannot order" and terminates.
3.	Action: User clicks "One-Click Buy".
4.	Processing:
o	POST request sent to /orders/one-click.
o	Stock and cart items are validated.
o	Payment Simulation: System attempts to process payment via Mock Service.
5.	Decision (Payment Success?):
o	If No: Displays "Insufficient Balance" error and redirects to Standard Checkout.
o	If Yes: Creates Order → Decreases Stock → Inserts “Transaction Log”.
6.	End: Renders the "Order Placed" confirmation page.

1.5 State Machine Diagrams
The state transitions of critical system entities are defined below.
1.5.1 Order & Transaction Lifecycle: Defines the financial lifecycle of an order and the Return (Audit Log) mechanism.

                                                        
Figure 1.15 — State Machine Diagram: Order & Transaction Lifecyle
•	States: InCart → PendingPayment → OrderCompleted (or PaymentFailed).
•	Return Flow: If the user requests a return after the order is completed, the state moves to ReturnRequested. If the administrator approves, it becomes Refunded, and a new return transaction is logged in the history, preserving the original record.
1.5.2 Filter Context State: Shows the state management of the smart filter interface.
       
Figure 1.16 — State Machine Diagram: Filter Context State
•	States: Starts as Idle and IncludeMode by default. Transitions to ExcludeMode based on user preference. With every filter selection, the system enters the FiltersApplied state, from where modes can be switched or filters can be cleared.
1.5.3 Product Lifecycle: Shows stock management and product statuses.
                                      
Figure 1.17 — State Machine Diagram: Product Lifecycle
•	Transitions: The product starts as Draft, then becomes Active. If stock runs out or falls below a critical threshold, it automatically transitions to the OutOfStock state. Discontinued products are moved to the Archived state.
1.5.4 Comment Moderation: Shows the quality control process for user reviews.

                                                        
Figure 1.18 — State Machine Diagram: Comment Moderation States

•	Flow: When a user submits a review, the state becomes Pending (Not visible on the site). With Product Manager approval, it transitions to Approved (Visible) or Rejected.

1.5.5 Theme Management Lifecycle: This model illustrates the system's visual personalization states controlled by the Admin.

             
Figure 1.19 — State Machine Diagram: Theme Management Lifecycle
•	States: The system initializes in LightMode (Default).
•	Transitions: The Admin can trigger a transition to DarkMode or SeasonalMode (e.g., for holidays) via the settings panel. The system can revert to Light or Dark modes when the seasonal period ends or is manually changed.
1.5.6 Company Owner Dashboard View States: This model defines the session and interaction lifecycle for the Company Owner role, emphasizing a read-only data experience.
                             
Figure 1.20 — State Machine Diagram: Company Owner Dashboard States
•	Session Flow: The state begins at LoggedOut. Upon successful authentication, it moves to LoggedIn, and then to ViewingDashboard when the dashboard page is accessed.
•	Data Interaction: When the owner changes date filters (e.g., "Last 30 Days"), the system temporarily enters the FilteringData state to recalculate charts before returning to ViewingDashboard. The lifecycle ends when the session expires or the user logs out.

1.6 Consistency & Verification Between Perspectives
The System Design phase ensures that all views of the system—context, interaction, structural, and behavioral—are internally consistent and fully aligned with the requirements defined in the SRS.
•	Context vs. Use Case Consistency:
All actors appearing in the Context Diagram (Customer, Admin, Product Manager, Company Owner, Payment Service) correspond directly to use cases in the Interaction Perspective.
•	Use Case vs. Sequence Diagram Consistency:
Each major use case (Filtering, Comparison, One-Click Buy, Review Moderation) is represented with at least one sequence diagram, showing the internal message flow required to fulfil that scenario.
•	Sequence vs. Class Diagram Consistency:
All service and controller objects referenced in the Sequence Diagrams exist as classes in the Structural Model (e.g., ProductService, OrderService, ReviewService).
•	Activity vs. State Diagram Consistency:
Each activity diagram represents a workflow, while the corresponding state diagrams represent the entity-level lifecycle behind those workflows (e.g., Order Activity ↔ Order State Machine). This ensures both process-level and object-level modeling accuracy.
1.7 System Design Conclusion
The System Design chapter successfully models the system from contextual, interaction, structural, and behavioral viewpoints. The use of UML diagrams—Context, Use Case, Sequence, Class, Activity, and State—ensures that all functional flows are traceable and operationally validated. These models collectively confirm that the system meets the analytical requirements derived during the Requirements Engineering phase.

________________________________________

2. ARCHITECTURAL DESIGN

2.1 Architectural Overview
The Online Tech Store project is built upon a modern, modular, and scalable 3-Tier Architecture, designed to support performance, maintainability, and extensibility across all functional modules.
2.2 Layered Architecture & Modules
•	Presentation Layer (Frontend) A Single Page Application (SPA) developed using React.js (v19.2.0). This layer adopts a component-based and event-driven interaction model, ensuring a fluid user experience during filtering, product comparison, and cart operations.
•	Application Layer (Backend API) A backend service implemented using ASP.NET Core Web API (v8.0), structured according to:
o	RESTful service principles
o	Clean Architecture guidelines
o	Repository Pattern 
o	Dependency Injection (DI) 
This layer contains core modules such as:
o	Product Module (listing, filtering, include/exclude engine)
o	Order Module (cart, One-Click Buy, order creation)
o	User & Role Management Module (RBAC)
o	Review Moderation Module
•	Data Layer (Database) A relational database stored in PostgreSQL (RDBMS). Data access is handled through Entity Framework Core (ORM) using either Code-First or DB-First approaches. Schema includes major data stores: ProductDB, UserDB, OrderDB, ReviewDB, TransactionDB.
2.3 Cross-Cutting Concerns
To ensure system-wide quality, the following cross-cutting mechanisms are integrated:
•	Security: JWT-based authentication , Role-Based Access Control (RBAC) , and password hashing (PBKDF2 or SHA-256).
•	Logging: Request & Response logs, Error/Exception logs, and audit trails for orders/refunds.
•	Validation: Backend input validation (FluentValidation) and frontend client-side schema validation.
•	Error Handling: Global exception middleware and graceful API error responses.
2.4 Architectural Quality Decisions
•	Performance: Database indexing for fast filtering and asynchronous API calls ensure <2s response time.
•	Scalability: All backend services are packaged in Docker containers, allowing independent API scaling.
•	Reliability: ACID-compliant transaction handling in PostgreSQL ensures data integrity.
•	Maintainability: Clear separation through the 3-tier structure and Repository patterns eases testing and extension.
•	Deployment Strategy: Docker-based environment isolation where API & DB run as separate containers.
2.5 Architectural Views (4+1 View Model)
The architectural description of the Online Tech Store is structured according to Philippe Kruchten’s 4+1 View Model, ensuring a comprehensive representation of both static and dynamic aspects of the system. This model validates that the architecture addresses the needs of all stakeholders.
1. Logical View (Conceptual Structure)
•	Focus: Represents the functional requirements and the conceptual organization of the system's domain entities.
•	Key Elements: The primary entities include User, Product, Order, Review, Cart, and Transaction.
•	Representation: This view is visualized via the Class Diagram (Section 1.3), defining the relationships (Association, Composition) and object properties.
2. Development View (Module Organization)
•	Focus: Illustrates the static structure of the source code, package organization, and development modules.
•	Frontend Structure (React SPA):
o	/components: Reusable UI elements (ProductCard, FilterSidebar, ComparePanel).
o	/pages: Route-based views (Home, Category, Dashboard).
o	/services: Axios instances handling API requests and Auth tokens.
•	Backend Structure (ASP.NET Core):
o	/Controllers: API Endpoints (ProductsController, OrdersController).
o	/Services: Business Logic Layer (ProductService, OrderService).
o	/Repositories: Data Access Layer implementing the Repository Pattern.
o	/Entities: Database models matching the Logical View.

3. Process View (Runtime Behavior)
•	Focus: Describes system concurrency, performance, and scalability.
•	Key Processes:
o	Asynchronous Filtering: Filter requests are processed asynchronously to ensure UI responsiveness.
o	Atomic Transactions: The "One-Click Buy" feature triggers ACID-compliant database transactions.
o	State Transitions: The Review Moderation pipeline involves definite state changes (Submitted → Pending → Approved/Rejected) as modeled in the State Diagrams.
4. Physical View (Deployment)
                                                 
Figure 2.1 — Deployment Diagram – Online Tech Store
•	Focus: Maps the software components to the hardware/infrastructure.
•	Topology: The system adopts a containerized approach running on a Docker Host.
o	Frontend: Nginx container serving static assets.
o	Backend: ASP.NET Core runtime container.
o	Database: PostgreSQL container.
•	Representation: This view is fully described in the Deployment Diagram (Section 3.5).
5. Scenarios View (Use Cases)
•	Focus: Validates the architecture by showing how components work together to satisfy user requirements.
•	Representation: This view is unified by the Use Case Diagram (Section 1.2.1) and detailed through the Sequence Diagrams (Section 1.2.2), specifically the "Dynamic Comparison" and "One-Click Buy" scenarios.

2.6 System Architecture Paradigm
The Online Tech Store system is built upon a combination of established architectural paradigms that together ensure scalability, maintainability, and modularity. Functionally, the Online Tech Store operates as a Transaction Processing System (TPS), given its heavy use of order creation, stock updates, and financial logging, all of which require strict ACID compliance.
1. Layered Architecture (3-Tier Model) — Primary Paradigm: The system follows a classical 3-Tier Architecture composed of:
•	Presentation Layer: React SPA
•	Application Layer: ASP.NET Core Web API
•	Data Layer: PostgreSQL
Each layer encapsulates its responsibilities and communicates only with adjacent layers, improving maintainability and enabling independent development.
2. Client–Server Architecture: The interaction between the React SPA and the ASP.NET Core backend follows the Client–Server paradigm. The client handles view rendering and user interaction, while the server manages business logic, authentication, and data persistence.
3. Component-Based Architecture (Frontend Paradigm): The frontend is constructed using React's component-based model, enabling reusability, state isolation, and event-driven updates. Critical UI features are implemented as independent components, including:
•	Filtering Engine
•	Compare Modal
•	Theme Manager
•	Cart Panel
4. Service–Repository Architecture (Backend Paradigm) The backend adopts a Clean Architecture–inspired pattern where Controllers delegate logic to Services, which in turn interact with Repository classes. This abstraction improves testability and reduces coupling between layers.
Together, these paradigms provide a stable foundation supporting modular development, extensibility, and performance optimization across the system.

2.7 Architectural Design Decisions
This section presents the academic and practical rationale behind the technical decisions made during the system design.
1.	Why 3-Tier Architecture?
o	Rationale: It allows independent development of the Frontend (React) and Backend (API). Changes in the presentation layer do not affect the Business Logic.
2.	Why Entity Framework Core (ORM)?
o	Rationale: It accelerates development by allowing object-oriented (LINQ) querying instead of raw SQL and minimizes SQL Injection risks.
3.	Why "Exclude" Filtering?
o	Rationale: Benchmarking analysis revealed that users struggle to "eliminate unwanted products." This feature was architecturally integrated to significantly improve User Experience (UX).

2.8 Architectural Design Conclusion
The Architectural Design chapter translates the system specifications into a deployable and maintainable technical framework. The adoption of a layered architecture, combined with component-based frontend design and service–repository backend patterns, ensures scalability, modularity, and robustness. The architectural views, constraints, risks, and deployment strategies provide a comprehensive blueprint for implementation and future extensibility.
