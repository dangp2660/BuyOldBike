# CORGI – Frontend UI Specification

## Project: Used Bicycle Marketplace

## Layer: Presentation Layer (WPF)

Technology: **WPF (.NET Desktop)**
Pattern: **UI Only – 3 Layer Architecture**

This document describes **UI screens, layout and components only**.
No business logic or data access is included.

---

# 1. UI Navigation Structure

```
MainWindow
│
├── LoginView
├── RegisterView
│
└── MainDashboard
     ├── BuyerArea
     │    ├── BicycleListView
     │    ├── BicycleDetailView
     │    ├── WishlistView
     │    ├── ChatView
     │    └── OrderView
     │
     ├── SellerArea
     │    ├── SellerDashboardView
     │    ├── CreateListingView
     │    ├── ManageListingView
     │    ├── SellerOrdersView
     │    └── SellerReviewsView
     │
     ├── InspectorArea
     │    ├── InspectorDashboardView
     │    ├── InspectionView
     │    └── DisputeSupportView
     │
     └── AdminArea
          ├── AdminDashboardView
          ├── UserManagementView
          ├── ListingModerationView
          ├── CategoryManagementView
          └── TransactionManagementView
```

---

# 2. Shared UI Components

Reusable components used across screens.

### Bicycle Card

Displays basic bicycle information.

Components:

```
Image
Title
Price
Brand
Condition
Inspection Badge
Favorite Button
```

---

### Search Bar

```
TextBox (search keyword)
Search Button
```

---

### Filter Panel

Filters include:

```
Brand
Price Range
Frame Size
Condition
Location
```

Controls:

```
ComboBox
Slider
CheckBox
```

---

### Image Gallery

Used in bicycle detail screen.

Components:

```
Main Image
Thumbnail List
Video Preview
```

---

### Rating Stars

Displays user ratings.

```
5 star icons
Average rating text
Review count
```

---

### Notification Toast

Small popup for user feedback.

```
Success Message
Error Message
Warning Message
```

---

# 3. Authentication Screens

## Login Screen

View: `LoginView.xaml`

Layout:

```
Logo

Email TextBox
Password PasswordBox

Login Button

Link: Register
```

---

## Register Screen

View: `RegisterView.xaml`

Fields:

```
Full Name
Email
Phone Number
Password
Confirm Password
```

Buttons:

```
Register
Back to Login
```

---

# 4. Buyer Interface

## Bicycle List Screen

View: `BicycleListView.xaml`

Layout:

```
Top Bar
   Search Bar
   Filter Button

Left Panel
   Filter Panel

Main Area
   Bicycle Card Grid

Bottom
   Pagination
```

---

## Bicycle Detail Screen

View: `BicycleDetailView.xaml`

Layout:

```
Left Section
   Image Gallery

Right Section
   Bicycle Information
      Title
      Price
      Brand
      Frame Size
      Condition
      Usage History

   Seller Information
      Seller Name
      Rating

   Buttons
      Chat Seller
      Add to Wishlist
      Place Deposit
```

---

## Wishlist Screen

View: `WishlistView.xaml`

Layout:

```
Saved Bicycle List

Each item:
   Bicycle Card
   Remove Button
```

---

## Chat Screen

View: `ChatView.xaml`

Layout:

```
Left Panel
   Conversation List

Right Panel
   Message Area

Bottom
   Message TextBox
   Send Button
```

---

## Order / Deposit Screen

View: `OrderView.xaml`

Components:

```
Bicycle Summary
Price
Deposit Amount

Buttons
Confirm Deposit
Cancel
```

---

# 5. Seller Interface

## Seller Dashboard

View: `SellerDashboardView.xaml`

Information panels:

```
Total Listings
Active Listings
Pending Orders
Recent Messages
```

Displayed using **dashboard cards**.

---

## Create Listing Screen

View: `CreateListingView.xaml`

Form fields:

```
Title
Description
Price
Brand
Frame Size
Condition
Usage History
```

Media upload:

```
Upload Images
Upload Video
```

Buttons:

```
Publish Listing
Cancel
```

---

## Manage Listings Screen

View: `ManageListingView.xaml`

Layout:

```
Listing Table

Columns:
Title
Price
Status
Views
Actions
```

Actions:

```
Edit
Hide
Delete
```

---

## Seller Orders Screen

View: `SellerOrdersView.xaml`

Table columns:

```
Buyer
Bicycle
Price
Deposit Status
Order Status
```

Buttons:

```
Accept Order
Reject Order
Mark Completed
```

---

## Seller Reviews Screen

View: `SellerReviewsView.xaml`

Components:

```
Average Rating
Total Reviews

Review List
   Buyer Name
   Rating
   Comment
```

---

# 6. Inspector Interface

## Inspector Dashboard

View: `InspectorDashboardView.xaml`

Cards:

```
Pending Inspections
Completed Inspections
Dispute Cases
```

---

## Inspection Screen

View: `InspectionView.xaml`

Checklist:

```
Frame Condition
Brake System
Drivetrain
Wheel Alignment
Suspension
```

Controls:

```
Pass / Fail Toggle
Notes TextBox
Upload Report Button
```

---

## Dispute Support Screen

View: `DisputeSupportView.xaml`

Layout:

```
Dispute List

Details Panel
   Bicycle Information
   Buyer Complaint
   Seller Response
   Inspector Conclusion
```

---

# 7. Admin Interface

## Admin Dashboard

View: `AdminDashboardView.xaml`

Statistics cards:

```
Total Users
Active Listings
Total Transactions
System Revenue
```

---

## User Management

View: `UserManagementView.xaml`

Table columns:

```
User Name
Email
Role
Status
```

Actions:

```
Suspend
Activate
View Profile
```

---

## Listing Moderation

View: `ListingModerationView.xaml`

Table:

```
Listing Title
Seller
Date Posted
Status
```

Buttons:

```
Approve
Reject
Remove
```

---

## Category Management

View: `CategoryManagementView.xaml`

Lists:

```
Bicycle Categories
Brands
Frame Sizes
```

Actions:

```
Add
Edit
Delete
```

---

## Transaction Management

View: `TransactionManagementView.xaml`

Table columns:

```
Transaction ID
Buyer
Seller
Amount
Status
Date
```

---

# 8. UI Folder Structure

Recommended WPF folder structure.

```
Presentation
│
├── Views
│   ├── Auth
│   ├── Buyer
│   ├── Seller
│   ├── Inspector
│   └── Admin
│
├── Components
│
├── Styles
│
└── Resources
```

---

# 9. UI Design Guidelines

Color theme:

```
Primary: Blue
Secondary: Gray
Success: Green
Error: Red
```

UI principles:

```
Clear navigation
Minimal clutter
Consistent layout
Responsive resizing
```

---

# 10. Accessibility

UI should support:

```
Keyboard navigation
Clear font size
High contrast elements
```
